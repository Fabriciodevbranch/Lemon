using LemonWriter.Application.Chapters.Queries;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Application.Snapshots.Commands;
using LemonWriter.Application.Snapshots.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SnapshotsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public SnapshotsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSnapshot(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSnapshotByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpGet("timeline/{chapterId:guid}")]
    public async Task<IActionResult> GetTimeline(Guid chapterId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetChapterTimelineQuery(chapterId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSnapshot([FromBody] CreateSnapshotRequest request, CancellationToken cancellationToken)
    {
        var authorId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _mediator.Send(
            new CreateSnapshotCommand(request.ChapterId, request.Content, request.SnapshotMessage, authorId),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error!.Message });
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreToSnapshot([FromBody] RestoreRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RestoreToSnapshotCommand(request.ChapterId, request.SnapshotId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost("grab-content")]
    public async Task<IActionResult> GrabContentFromSnapshot([FromBody] GrabContentRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GrabContentFromSnapshotCommand(request.TargetChapterId, request.SnapshotId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpGet("compare")]
    public async Task<IActionResult> CompareSnapshots([FromQuery] Guid baseId, [FromQuery] Guid compareId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompareSnapshotsQuery(baseId, compareId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }
}

public record CreateSnapshotRequest(Guid ChapterId, string Content, string SnapshotMessage);
public record RestoreRequest(Guid ChapterId, Guid SnapshotId);
public record GrabContentRequest(Guid TargetChapterId, Guid SnapshotId);
