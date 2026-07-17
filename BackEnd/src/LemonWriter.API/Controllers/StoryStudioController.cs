using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Authorize]
[Route("api/books/{bookId:guid}/studio")]
public sealed class StoryStudioController(
    IStudioEntryService entries,
    ITimelineService timeline,
    IRelationshipService relationships,
    IStoryMetricsService metrics,
    ICurrentUserService currentUser,
    IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet("{type}")]
    public async Task<IActionResult> List(Guid bookId, string type, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        if (!entries.IsSupportedType(type)) return BadRequest(new { message = "Unknown studio entry type." });
        return Ok(await entries.ListAsync(bookId, type, ct));
    }

    [HttpPost("{type}")]
    [RequestSizeLimit(7 * 1024 * 1024)]
    public async Task<IActionResult> Create(Guid bookId, string type, CreateStudioEntry request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await entries.CreateAsync(bookId, type, request.ToDto(), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { bookId, type }, result.Value)
            : BadRequest(new { message = result.Error!.Message });
    }

    [HttpPatch("goals/{id:guid}/progress")]
    public async Task<IActionResult> UpdateGoalProgress(Guid bookId, Guid id, GoalProgressRequest request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await entries.UpdateGoalProgressAsync(bookId, id, request.Progress, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPut("timeline/order")]
    public async Task<IActionResult> ReorderTimeline(Guid bookId, ReorderTimelineRequest request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await timeline.ReorderAsync(bookId, request.Ids, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.Error!.Message });
    }

    [HttpDelete("entries/{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid bookId, Guid id, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await entries.DeleteEntryAsync(bookId, id, ct);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpGet("relationships")]
    public async Task<IActionResult> Relationships(Guid bookId, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        return Ok(await relationships.ListRelationshipsAsync(bookId, ct));
    }

    [HttpPost("relationships")]
    public async Task<IActionResult> CreateRelationship(Guid bookId, CreateRelationship request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await relationships.CreateRelationshipAsync(bookId, new(request.From, request.To, request.Label, request.Tone), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { message = result.Error!.Message });
    }

    [HttpDelete("relationships/{id:guid}")]
    public async Task<IActionResult> DeleteRelationship(Guid bookId, Guid id, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await relationships.DeleteRelationshipAsync(bookId, id, ct);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics(Guid bookId, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        if (currentUser.UserId is not Guid userId) return Unauthorized();
        return Ok(await metrics.GetMetricsAsync(bookId, userId, ct));
    }

    private Task<bool> OwnsBook(Guid bookId, CancellationToken ct) => currentUser.UserId is Guid userId
        ? authorization.OwnsBookAsync(userId, bookId, ct) : Task.FromResult(false);
}

public record CreateStudioEntry(string Name, string? Summary, string? Details, string? Motivation, string? Plot, string? Image,
    string? EventDate, string? Impact, string? CharacterIds, string? ObjectIds, string? PlaceIds, int? GoalTarget, int? GoalProgress)
{
    public CreateStudioEntryDto ToDto() => new(Name, Summary, Details, Motivation, Plot, Image, EventDate, Impact,
        CharacterIds, ObjectIds, PlaceIds, GoalTarget, GoalProgress);
}
public record GoalProgressRequest(int Progress);
public record CreateRelationship(Guid From, Guid To, string? Label, string? Tone);
public record ReorderTimelineRequest(IReadOnlyList<Guid> Ids);
