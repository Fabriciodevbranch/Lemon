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

    public BooksController(IMediator mediator) => _mediator = mediator;

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
}

public record CreateBookRequest(
    string Title, string Description, Guid AuthorId, string AuthorName,
    string? ISBN = null, string? INBR = null, string? CoverImageUrl = null,
    bool IsSeries = false, int? SeriesVolume = null, string? SeriesName = null);

public record UpdateBookRequest(
    string Title, string Description, string AuthorName,
    string? ISBN = null, string? INBR = null, string? CoverImageUrl = null,
    bool IsSeries = false, int? SeriesVolume = null, string? SeriesName = null);
