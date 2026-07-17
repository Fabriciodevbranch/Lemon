using LemonWriter.Application.Books.Commands;
using LemonWriter.Application.Books.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LemonWriter.Application.Common.Interfaces;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILibraryQueryService _library;
    private readonly ICurrentUserService _currentUser;
    private readonly IResourceAuthorizationService _authorization;
    public BooksController(IMediator mediator, ILibraryQueryService library, ICurrentUserService currentUser, IResourceAuthorizationService authorization)
    { _mediator = mediator; _library = library; _currentUser = currentUser; _authorization = authorization; }

    [HttpGet]
    public async Task<IActionResult> GetBooks([FromQuery] Guid authorId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var result = await _mediator.Send(new GetBooksQuery(userId.Value), cancellationToken);
        if (result.IsFailure) return NotFound(new { error = result.Error!.Message });
        var stats = await _library.GetBookStatsAsync(result.Value!.Select(x => x.Id), cancellationToken);
        return Ok(result.Value!.Select(book => new
        {
            book.Id, book.Title, book.Description, book.AuthorId, book.ISBN, book.INBR, book.AuthorName,
            book.CoverImageUrl, book.IsSeries, book.SeriesVolume, book.SeriesName, book.CreatedAt, book.UpdatedAt,
            ChapterCount = stats.GetValueOrDefault(book.Id)?.ChapterCount ?? 0,
            WordCount = stats.GetValueOrDefault(book.Id)?.WordCount ?? 0,
            Progress = stats.GetValueOrDefault(book.Id)?.Progress ?? 0
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBook(Guid id, CancellationToken cancellationToken)
    {
        if (!await Owns(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new GetBookByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var command = new CreateBookCommand(
            request.Title, request.Description, userId.Value, request.AuthorName,
            request.ISBN, request.INBR, request.CoverImageUrl,
            request.IsSeries, request.SeriesVolume, request.SeriesName);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBook), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error!.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
    {
        if (!await Owns(id, cancellationToken)) return NotFound();
        var command = new UpdateBookCommand(
            id, request.Title, request.Description, request.AuthorName,
            request.ISBN, request.INBR, request.CoverImageUrl,
            request.IsSeries, request.SeriesVolume, request.SeriesName);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        if (!await Owns(id, cancellationToken)) return NotFound();
        var result = await _mediator.Send(new DeleteBookCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost("{id:guid}/cover")]
    public async Task<IActionResult> UploadCover(Guid id, IFormFile cover, CancellationToken cancellationToken)
    {
        if (!await Owns(id, cancellationToken)) return NotFound();
        if (cover is null || cover.Length == 0)
            return BadRequest(new { error = "No file provided." });

        if (cover.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Cover images must be 5 MB or smaller." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(cover.ContentType))
            return BadRequest(new { error = "Invalid file type. Only JPEG, PNG, GIF and WebP are allowed." });

        await using var stream = new MemoryStream();
        await cover.CopyToAsync(stream, cancellationToken);
        var coverImageUrl = $"data:{cover.ContentType};base64,{Convert.ToBase64String(stream.ToArray())}";

        var bookResult = await _mediator.Send(new GetBookByIdQuery(id), cancellationToken);
        if (bookResult.IsFailure)
            return NotFound(new { error = "Book not found." });

        var book = bookResult.Value!;
        var updateCommand = new UpdateBookCommand(
            id, book.Title, book.Description, book.AuthorName,
            book.ISBN, book.INBR, coverImageUrl,
            book.IsSeries, book.SeriesVolume, book.SeriesName);
        var updateResult = await _mediator.Send(updateCommand, cancellationToken);

        return updateResult.IsSuccess
            ? Ok(new { coverImageUrl })
            : BadRequest(new { error = updateResult.Error!.Message });
    }

    private Task<bool> Owns(Guid bookId, CancellationToken ct) => _currentUser.UserId is Guid userId
        ? _authorization.OwnsBookAsync(userId, bookId, ct) : Task.FromResult(false);
}

public record CreateBookRequest(
    string Title, string Description, Guid AuthorId, string AuthorName,
    string? ISBN = null, string? INBR = null, string? CoverImageUrl = null,
    bool IsSeries = false, int? SeriesVolume = null, string? SeriesName = null);

public record UpdateBookRequest(
    string Title, string Description, string AuthorName,
    string? ISBN = null, string? INBR = null, string? CoverImageUrl = null,
    bool IsSeries = false, int? SeriesVolume = null, string? SeriesName = null);
