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
public class DraftsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public DraftsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDrafts([FromQuery] Guid chapterId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDraftsByChapterQuery(chapterId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDraft(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDraftByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraft([FromBody] CreateDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateDraftCommand(request.ChapterId, request.Title, request.InitialContent), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDraft), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error!.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] UpdateDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateDraftCommand(id, request.Title, request.Content), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishDraft(Guid id, [FromBody] PublishDraftRequest request, CancellationToken cancellationToken)
    {
        var authorId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new PublishDraftCommand(id, request.PublishMessage, authorId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error!.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDraft(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteDraftCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error!.Message });
    }
}

public record CreateDraftRequest(Guid ChapterId, string Title, string InitialContent = "");
public record UpdateDraftRequest(string Title, string Content);
public record PublishDraftRequest(string PublishMessage);
