using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Application.Drafts.Commands;
using LemonWriter.Application.Drafts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Endpoints for draft creation, updates, publishing, and deletion.
/// </summary>
public class DraftsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IResourceAuthorizationService _authorization;

    public DraftsController(IMediator mediator, ICurrentUserService currentUserService, IResourceAuthorizationService authorization)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    [HttpGet]
    /// <summary>
    /// Lists drafts for a chapter.
    /// </summary>
    public async Task<IActionResult> GetDrafts([FromQuery] Guid chapterId, CancellationToken cancellationToken)
    {
        if (!await OwnsChapter(chapterId, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new GetDraftsByChapterQuery(chapterId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpGet("{id:guid}")]
    /// <summary>
    /// Gets a draft by identifier.
    /// </summary>
    public async Task<IActionResult> GetDraft(Guid id, CancellationToken cancellationToken)
    {
        if (!await OwnsDraft(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new GetDraftByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    /// <summary>
    /// Creates a draft in a chapter.
    /// </summary>
    public async Task<IActionResult> CreateDraft([FromBody] CreateDraftRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsChapter(request.ChapterId, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new CreateDraftCommand(request.ChapterId, request.Title, request.InitialContent), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDraft), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error!.Message });
    }

    [HttpPut("{id:guid}")]
    /// <summary>
    /// Updates draft title and content.
    /// </summary>
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] UpdateDraftRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsDraft(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new UpdateDraftCommand(id, request.Title, request.Content), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost("{id:guid}/publish")]
    /// <summary>
    /// Publishes a draft to the underlying chapter.
    /// </summary>
    public async Task<IActionResult> PublishDraft(Guid id, [FromBody] PublishDraftRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsDraft(id, cancellationToken)) return NotFound();
        var authorId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new PublishDraftCommand(id, request.PublishMessage, authorId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error!.Message });
    }

    [HttpDelete("{id:guid}")]
    /// <summary>
    /// Deletes a draft.
    /// </summary>
    public async Task<IActionResult> DeleteDraft(Guid id, CancellationToken cancellationToken)
    {
        if (!await OwnsDraft(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new DeleteDraftCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error!.Message });
    }

    private Task<bool> OwnsChapter(Guid id, CancellationToken ct)
        => _currentUserService.UserId is Guid userId
            ? _authorization.OwnsChapterAsync(userId, id, ct)
            : Task.FromResult(false);

    private Task<bool> OwnsDraft(Guid id, CancellationToken ct)
        => _currentUserService.UserId is Guid userId
            ? _authorization.OwnsDraftAsync(userId, id, ct)
            : Task.FromResult(false);
}

public record CreateDraftRequest(Guid ChapterId, string Title, string InitialContent = "");
public record UpdateDraftRequest(string Title, string Content);
public record PublishDraftRequest(string PublishMessage);
