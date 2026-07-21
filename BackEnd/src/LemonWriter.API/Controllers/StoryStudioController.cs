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

    [HttpPost("gallery/batch")]
    [RequestSizeLimit(72 * 1024 * 1024)]
    public async Task<IActionResult> CreateGalleryBatch(Guid bookId, CreateGalleryBatch request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await entries.CreateGalleryBatchAsync(bookId,
            new(request.CollectionName, request.Items.Select(x => x.ToDto()).ToList()), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { message = result.Error!.Message });
    }

    [HttpPut("entries/{id:guid}/metadata")]
    public async Task<IActionResult> UpdateMetadata(Guid bookId, Guid id, UpdateStudioEntryMetadata request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await entries.UpdateMetadataAsync(bookId, id, new(request.Name, request.Summary, request.Details), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error == LemonWriter.Application.Common.Errors.Error.NotFound
            ? NotFound() : BadRequest(new { message = result.Error!.Message });
    }

    [HttpPut("characters/{id:guid}/profile")]
    public async Task<IActionResult> UpdateCharacterProfile(Guid bookId, Guid id, UpdateCharacterProfile request, CancellationToken ct)
    {
        if (!await OwnsBook(bookId, ct)) return NotFound();
        var result = await entries.UpdateCharacterProfileAsync(bookId, id, request.ToDto(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error == LemonWriter.Application.Common.Errors.Error.NotFound
            ? NotFound() : BadRequest(new { message = result.Error!.Message });
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
    string? EventDate, string? Impact, string? CharacterIds, string? ObjectIds, string? PlaceIds, int? GoalTarget, int? GoalProgress,
    string? StoryRole = null, Guid? PortraitMediaId = null)
{
    public CreateStudioEntryDto ToDto() => new(Name, Summary, Details, Motivation, Plot, Image, EventDate, Impact,
        CharacterIds, ObjectIds, PlaceIds, GoalTarget, GoalProgress, StoryRole, PortraitMediaId);
}
public record GoalProgressRequest(int Progress);
public record CreateGalleryBatch(string? CollectionName, IReadOnlyList<CreateStudioEntry> Items);
public record UpdateStudioEntryMetadata(string Name, string? Summary, string? Details);
public record UpdateCharacterProfile(string Name, string? Summary, string? StoryRole, string? CharacterStatus,
    string? Age, string? Pronouns, string? Aliases, Guid? PortraitMediaId, string? ExternalGoal, string? InternalNeed,
    string? Fear, string? Secret, string? InternalConflict, string? ExternalConflict, string? NarrativeFunction,
    string? ArcSummary, string? StartingState, string? TurningPoint, string? EndingState, string? Notes)
{
    public UpdateCharacterProfileDto ToDto() => new(Name, Summary, StoryRole, CharacterStatus, Age, Pronouns, Aliases,
        PortraitMediaId, ExternalGoal, InternalNeed, Fear, Secret, InternalConflict, ExternalConflict, NarrativeFunction,
        ArcSummary, StartingState, TurningPoint, EndingState, Notes);
}
public record CreateRelationship(Guid From, Guid To, string? Label, string? Tone);
public record ReorderTimelineRequest(IReadOnlyList<Guid> Ids);
