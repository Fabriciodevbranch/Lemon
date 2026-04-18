using LemonWriter.Application.Books.Commands;
using LemonWriter.Application.Books.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public BooksController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetBooks([FromQuery] Guid authorId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBooksQuery(authorId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error!.Message });
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

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(cover.ContentType))
            return BadRequest(new { error = "Invalid file type. Only JPEG, PNG, GIF and WebP are allowed." });

        var allowedExtensions = new Dictionary<string, string>
        {
            { "image/jpeg", ".jpg" },
            { "image/png", ".png" },
            { "image/gif", ".gif" },
            { "image/webp", ".webp" }
        };
        var ext = allowedExtensions[cover.ContentType];

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var coversDir = Path.Combine(webRoot, "covers");
        Directory.CreateDirectory(coversDir);

        // Remove any existing cover files for this book
        foreach (var existing in Directory.GetFiles(coversDir, $"{id}.*"))
        {
            System.IO.File.Delete(existing);
        }

        var fileName = $"{id}{ext}";
        var filePath = Path.Combine(coversDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await cover.CopyToAsync(stream, cancellationToken);
        }

        var coverImageUrl = $"/covers/{fileName}";

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
