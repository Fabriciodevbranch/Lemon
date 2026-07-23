using LemonWriter.Application.Chapters.Commands;
using LemonWriter.Application.Chapters.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LemonWriter.Application.Common.Interfaces;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Endpoints for chapter management under user-owned books.
/// </summary>
public class ChaptersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IResourceAuthorizationService _authorization;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaptersController"/> class.
    /// </summary>
    public ChaptersController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IResourceAuthorizationService authorization)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    [HttpGet]
    /// <summary>
    /// Lists chapters for a given book.
    /// </summary>
    public async Task<IActionResult> GetChapters([FromQuery] Guid bookId, CancellationToken cancellationToken)
    {
        if (!await OwnsBook(bookId, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new GetChaptersByBookQuery(bookId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpGet("{id:guid}")]
    /// <summary>
    /// Gets one chapter by identifier.
    /// </summary>
    public async Task<IActionResult> GetChapter(Guid id, CancellationToken cancellationToken)
    {
        if (!await OwnsChapter(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new GetChapterByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    /// <summary>
    /// Creates a chapter for a book owned by the current user.
    /// </summary>
    public async Task<IActionResult> CreateChapter([FromBody] CreateChapterRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsBook(request.BookId, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new CreateChapterCommand(request.BookId, request.Title, request.Order), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetChapter), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error!.Message });
    }

    [HttpPut("{id:guid}")]
    /// <summary>
    /// Updates an existing chapter.
    /// </summary>
    public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] UpdateChapterRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsChapter(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new UpdateChapterCommand(id, request.Title, request.Content, request.Order), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpDelete("{id:guid}")]
    /// <summary>
    /// Deletes an existing chapter.
    /// </summary>
    public async Task<IActionResult> DeleteChapter(Guid id, CancellationToken cancellationToken)
    {
        if (!await OwnsChapter(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new DeleteChapterCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error!.Message });
    }

    private Task<bool> OwnsBook(Guid id, CancellationToken ct)
        => _currentUser.UserId is Guid userId
            ? _authorization.OwnsBookAsync(userId, id, ct)
            : Task.FromResult(false);

    private Task<bool> OwnsChapter(Guid id, CancellationToken ct)
        => _currentUser.UserId is Guid userId
            ? _authorization.OwnsChapterAsync(userId, id, ct)
            : Task.FromResult(false);
}

public record CreateChapterRequest(Guid BookId, string Title, int Order);
public record UpdateChapterRequest(string Title, string Content, int Order);
