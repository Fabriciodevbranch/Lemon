using Grpc.Core;
using LemonWriter.Application.Books.Commands;
using LemonWriter.Application.Books.Queries;
using LemonWriter.Grpc;
using MediatR;

namespace LemonWriter.Infrastructure.gRPC;

public class BooksGrpcService : BooksService.BooksServiceBase
{
    private readonly IMediator _mediator;

    public BooksGrpcService(IMediator mediator) => _mediator = mediator;

    public override async Task<GetBooksResponse> GetBooks(GetBooksRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthorId, out var authorId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid author ID."));

        var result = await _mediator.Send(new GetBooksQuery(authorId), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.Internal, result.Error!.Message));

        var response = new GetBooksResponse();
        response.Books.AddRange(result.Value!.Select(MapToProto));
        return response;
    }

    public override async Task<BookResponse> GetBook(GetBookRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid book ID."));

        var result = await _mediator.Send(new GetBookByIdQuery(id), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.NotFound, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    public override async Task<BookResponse> CreateBook(CreateBookRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AuthorId, out var authorId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid author ID."));

        var command = new CreateBookCommand(
            request.Title, request.Description, authorId, request.AuthorName,
            request.Isbn, request.Inbr, request.CoverImageUrl,
            request.IsSeries, request.SeriesVolume, request.SeriesName);

        var result = await _mediator.Send(command, context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.Internal, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    public override async Task<BookResponse> UpdateBook(UpdateBookRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid book ID."));

        var command = new UpdateBookCommand(
            id, request.Title, request.Description, request.AuthorName,
            request.Isbn, request.Inbr, request.CoverImageUrl,
            request.IsSeries, request.SeriesVolume, request.SeriesName);

        var result = await _mediator.Send(command, context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.NotFound, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    public override async Task<DeleteBookResponse> DeleteBook(DeleteBookRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid book ID."));

        var result = await _mediator.Send(new DeleteBookCommand(id), context.CancellationToken);
        return new DeleteBookResponse { Success = result.IsSuccess };
    }

    private static BookResponse MapToProto(Application.Common.DTOs.BookDto dto) => new()
    {
        Id = dto.Id.ToString(),
        Title = dto.Title,
        Description = dto.Description,
        AuthorId = dto.AuthorId.ToString(),
        Isbn = dto.ISBN ?? string.Empty,
        Inbr = dto.INBR ?? string.Empty,
        AuthorName = dto.AuthorName,
        CoverImageUrl = dto.CoverImageUrl ?? string.Empty,
        IsSeries = dto.IsSeries,
        SeriesVolume = dto.SeriesVolume ?? 0,
        SeriesName = dto.SeriesName ?? string.Empty,
        CreatedAt = dto.CreatedAt.ToString("O"),
        UpdatedAt = dto.UpdatedAt.ToString("O")
    };
}
