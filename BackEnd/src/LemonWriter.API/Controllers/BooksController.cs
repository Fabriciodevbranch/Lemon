using LemonWriter.Application.Books.Commands;
using LemonWriter.Application.Books.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly LemonDbContext _db;
    public BooksController(IMediator mediator, LemonDbContext db) { _mediator = mediator; _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetBooks([FromQuery] Guid authorId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBooksQuery(authorId), cancellationToken);
        if (result.IsFailure) return NotFound(new { error = result.Error!.Message });
        var bookIds = result.Value!.Select(x => x.Id).ToArray();
        var chapters = await _db.Chapters.AsNoTracking().Where(x => bookIds.Contains(x.BookId))
            .Select(x => new { x.BookId, x.CurrentContent }).ToListAsync(cancellationToken);
        var stats = chapters.GroupBy(x => x.BookId).ToDictionary(x => x.Key, x => new
        {
            ChapterCount = x.Count(),
            WordCount = x.Sum(chapter => string.IsNullOrWhiteSpace(chapter.CurrentContent) ? 0 : chapter.CurrentContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length)
        });
        return Ok(result.Value!.Select(book => new
        {
            book.Id, book.Title, book.Description, book.AuthorId, book.ISBN, book.INBR, book.AuthorName,
            book.CoverImageUrl, book.IsSeries, book.SeriesVolume, book.SeriesName, book.CreatedAt, book.UpdatedAt,
            ChapterCount = stats.GetValueOrDefault(book.Id)?.ChapterCount ?? 0,
            WordCount = stats.GetValueOrDefault(book.Id)?.WordCount ?? 0,
            Progress = Math.Min(100, (stats.GetValueOrDefault(book.Id)?.WordCount ?? 0) / 800)
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBook(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBookByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBookCommand(
            request.Title, request.Description, request.AuthorId, request.AuthorName,
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
        var result = await _mediator.Send(new DeleteBookCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error!.Message });
    }

    [HttpPost("{id:guid}/cover")]
    public async Task<IActionResult> UploadCover(Guid id, IFormFile cover, CancellationToken cancellationToken)
    {
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
}

public record CreateBookRequest(
    string Title, string Description, Guid AuthorId, string AuthorName,
    string? ISBN = null, string? INBR = null, string? CoverImageUrl = null,
    bool IsSeries = false, int? SeriesVolume = null, string? SeriesName = null);

public record UpdateBookRequest(
    string Title, string Description, string AuthorName,
    string? ISBN = null, string? INBR = null, string? CoverImageUrl = null,
    bool IsSeries = false, int? SeriesVolume = null, string? SeriesName = null);
