using LemonWriter.Application.Export.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LemonWriter.Application.Common.Interfaces;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Endpoints for exporting books to supported formats.
/// </summary>
public class ExportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IResourceAuthorizationService _authorization;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportController"/> class.
    /// </summary>
    public ExportController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IResourceAuthorizationService authorization)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    [HttpGet("{bookId:guid}")]
    [HttpGet("/api/books/{bookId:guid}/export/{format}")]
    /// <summary>
    /// Exports a book owned by the current user as EPUB or PDF.
    /// </summary>
    public async Task<IActionResult> ExportBook(Guid bookId, string format = "epub", CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId
            || !await _authorization.OwnsBookAsync(userId, bookId, cancellationToken))
        {
            return NotFound();
        }

        var result = await _mediator.Send(new ExportBookCommand(bookId, format), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!.Code == "NOT_FOUND"
                ? NotFound(new { error = result.Error.Message })
                : BadRequest(new { error = result.Error.Message });
        }

        var contentType = format.ToLowerInvariant() == "pdf" ? "application/pdf" : "application/epub+zip";
        var fileName = $"book_{bookId}.{format.ToLowerInvariant()}";
        return File(result.Value!, contentType, fileName);
    }
}
