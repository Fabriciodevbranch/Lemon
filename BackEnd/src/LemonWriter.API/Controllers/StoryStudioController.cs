using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.API.Controllers;

[ApiController]
[Authorize]
[Route("api/books/{bookId:guid}/studio")]
public sealed class StoryStudioController : ControllerBase
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
        { "characters", "places", "objects", "timeline", "goals", "lore", "magic", "research", "gallery" };
    private readonly LemonDbContext _db;
    public StoryStudioController(LemonDbContext db) => _db = db;

    [HttpGet("{type}")]
    public async Task<IActionResult> List(Guid bookId, string type, CancellationToken ct)
    {
        if (!Types.Contains(type)) return BadRequest(new { message = "Unknown studio entry type." });
        var query = _db.StoryStudioEntries.AsNoTracking().Where(x => x.BookId == bookId && x.Type == type.ToLower());
        var items = type.Equals("timeline", StringComparison.OrdinalIgnoreCase)
            ? await query.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt).ToListAsync(ct)
            : await query.OrderByDescending(x => x.UpdatedAt).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{type}")]
    [RequestSizeLimit(7 * 1024 * 1024)]
    public async Task<IActionResult> Create(Guid bookId, string type, CreateStudioEntry request, CancellationToken ct)
    {
        if (!Types.Contains(type)) return BadRequest(new { message = "Unknown studio entry type." });
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Name is required." });
        if (request.Image?.Length > 7_000_000) return BadRequest(new { message = "Image is too large." });
        var item = StoryStudioEntry.Create(bookId, type.ToLower(), request.Name, request.Summary,
            request.Details, request.Motivation, request.Plot, request.Image, request.EventDate,
            request.Impact, SanitizeIds(request.CharacterIds), SanitizeIds(request.ObjectIds), SanitizeIds(request.PlaceIds),
            request.GoalTarget, request.GoalProgress ?? 0);
        if (type.Equals("timeline", StringComparison.OrdinalIgnoreCase))
            item.SetSortOrder(await _db.StoryStudioEntries.CountAsync(x => x.BookId == bookId && x.Type == "timeline", ct));
        _db.StoryStudioEntries.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { bookId, type }, item);
    }

    [HttpPatch("goals/{id:guid}/progress")]
    public async Task<IActionResult> UpdateGoalProgress(Guid bookId, Guid id, GoalProgressRequest request, CancellationToken ct)
    {
        var goal = await _db.StoryStudioEntries.SingleOrDefaultAsync(x => x.BookId == bookId && x.Id == id && x.Type == "goals", ct);
        if (goal is null) return NotFound();
        goal.SetGoalProgress(request.Progress);
        await _db.SaveChangesAsync(ct);
        return Ok(goal);
    }

    private static string? SanitizeIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var ids = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => Guid.TryParse(x, out _)).Distinct().ToArray();
        return ids.Length == 0 ? null : string.Join(',', ids);
    }

    [HttpPut("timeline/order")]
    public async Task<IActionResult> ReorderTimeline(Guid bookId, ReorderTimelineRequest request, CancellationToken ct)
    {
        var events = await _db.StoryStudioEntries.Where(x => x.BookId == bookId && x.Type == "timeline" && request.Ids.Contains(x.Id)).ToListAsync(ct);
        if (events.Count != request.Ids.Count || request.Ids.Distinct().Count() != request.Ids.Count)
            return BadRequest(new { message = "The timeline order contains invalid events." });
        var byId = events.ToDictionary(x => x.Id);
        for (var index = 0; index < request.Ids.Count; index++) byId[request.Ids[index]].SetSortOrder(index);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("entries/{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid bookId, Guid id, CancellationToken ct)
    {
        var item = await _db.StoryStudioEntries.FirstOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (item is null) return NotFound();
        _db.StoryStudioEntries.Remove(item);
        await _db.StoryRelationships.Where(x => x.BookId == bookId && (x.FromEntryId == id || x.ToEntryId == id)).ExecuteDeleteAsync(ct);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("relationships")]
    public async Task<IActionResult> Relationships(Guid bookId, CancellationToken ct) => Ok(
        await _db.StoryRelationships.AsNoTracking().Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.CreatedAt).Select(x => new
            {
                x.Id, From = x.FromEntryId, To = x.ToEntryId, x.Label, x.Tone
            }).ToListAsync(ct));

    [HttpPost("relationships")]
    public async Task<IActionResult> CreateRelationship(Guid bookId, CreateRelationship request, CancellationToken ct)
    {
        if (request.From == request.To) return BadRequest(new { message = "Choose two different characters." });
        var count = await _db.StoryStudioEntries.CountAsync(x => x.BookId == bookId &&
            x.Type == "characters" && (x.Id == request.From || x.Id == request.To), ct);
        if (count != 2) return BadRequest(new { message = "Both characters must belong to this book." });
        var relation = StoryRelationship.Create(bookId, request.From, request.To, request.Label, request.Tone);
        _db.StoryRelationships.Add(relation);
        await _db.SaveChangesAsync(ct);
        return Ok(new { relation.Id, From = relation.FromEntryId, To = relation.ToEntryId, relation.Label, relation.Tone });
    }

    [HttpDelete("relationships/{id:guid}")]
    public async Task<IActionResult> DeleteRelationship(Guid bookId, Guid id, CancellationToken ct)
    {
        var relation = await _db.StoryRelationships.FirstOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (relation is null) return NotFound();
        _db.Remove(relation); await _db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics(Guid bookId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var enabled = await _db.Users.Where(x => x.Id == userId).Select(x => x.StoryMetricsEnabled).SingleOrDefaultAsync(ct);
        if (!enabled) return Ok(new { enabled = false });

        var entries = await _db.StoryStudioEntries.AsNoTracking().Where(x => x.BookId == bookId).ToListAsync(ct);
        var counts = entries.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
        var relationships = await _db.StoryRelationships.CountAsync(x => x.BookId == bookId, ct);
        int Get(string key) => counts.GetValueOrDefault(key);
        var characters = entries.Where(x => x.Type == "characters").ToList();
        var places = entries.Where(x => x.Type == "places").ToList();
        var objects = entries.Where(x => x.Type == "objects").ToList();
        var timeline = entries.Where(x => x.Type == "timeline").ToList();
        var relationshipEdges = await _db.StoryRelationships.AsNoTracking().Where(x => x.BookId == bookId)
            .Select(x => new { x.FromEntryId, x.ToEntryId }).ToListAsync(ct);
        var linkedIds = relationshipEdges.SelectMany(x => new[] { x.FromEntryId, x.ToEntryId }).Distinct().ToList();
        static int Coverage<T>(IReadOnlyCollection<T> all, Func<T, bool> complete) => all.Count == 0 ? 0 : (int)Math.Round(all.Count(complete) * 100d / all.Count);
        var characterCoverage = Coverage(characters, x => !string.IsNullOrWhiteSpace(x.Details) && (!string.IsNullOrWhiteSpace(x.Motivation) || !string.IsNullOrWhiteSpace(x.Plot)));
        var relationshipCoverage = characters.Count == 0 ? 0 : (int)Math.Round(linkedIds.Count * 100d / characters.Count);
        var timelineCompleteness = Coverage(timeline, x => !string.IsNullOrWhiteSpace(x.Summary) && !string.IsNullOrWhiteSpace(x.Details));
        var worldbuilding = Math.Min(100, Get("characters") * 10 + Get("places") * 8 + Get("objects") * 5 + Get("lore") * 4 + Get("magic") * 4);
        return Ok(new { enabled = true, characters = Get("characters"), places = Get("places"), objects = Get("objects"), gallery = Get("gallery"), relationships,
            completeness = worldbuilding, storyCompleteness = (worldbuilding + characterCoverage + relationshipCoverage + timelineCompleteness) / 4,
            characterCoverage, relationshipCoverage = Math.Min(100, relationshipCoverage), timelineCompleteness,
            orphanCharacters = Math.Max(0, characters.Count - linkedIds.Count),
            emptyLocations = places.Count(x => string.IsNullOrWhiteSpace(x.Details)),
            unusedObjects = objects.Count(x => string.IsNullOrWhiteSpace(x.Plot)) });
    }
}

public record CreateStudioEntry(string Name, string? Summary, string? Details, string? Motivation, string? Plot, string? Image,
    string? EventDate, string? Impact, string? CharacterIds, string? ObjectIds, string? PlaceIds, int? GoalTarget, int? GoalProgress);
public record GoalProgressRequest(int Progress);
public record CreateRelationship(Guid From, Guid To, string? Label, string? Tone);
public record ReorderTimelineRequest(IReadOnlyList<Guid> Ids);
