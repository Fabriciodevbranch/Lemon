using LemonWriter.Application.Export.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExportController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{bookId:guid}")]
    [HttpGet("/api/books/{bookId:guid}/export/{format}")]
    public async Task<IActionResult> ExportBook(Guid bookId, string format = "epub", CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExportBookCommand(bookId, format), cancellationToken);
        if (result.IsFailure)
            return result.Error!.Code == "NOT_FOUND"
                ? NotFound(new { error = result.Error.Message })
                : BadRequest(new { error = result.Error.Message });

        var contentType = format.ToLowerInvariant() == "pdf" ? "application/pdf" : "application/epub+zip";
        var fileName = $"book_{bookId}.{format.ToLowerInvariant()}";
        return File(result.Value!, contentType, fileName);
    }
}
