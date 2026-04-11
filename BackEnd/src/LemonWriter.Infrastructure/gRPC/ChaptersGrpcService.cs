using Grpc.Core;
using LemonWriter.Application.Chapters.Commands;
using LemonWriter.Application.Chapters.Queries;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Application.Snapshots.Commands;
using LemonWriter.Grpc;
using MediatR;

namespace LemonWriter.Infrastructure.gRPC;

public class ChaptersGrpcService : ChaptersService.ChaptersServiceBase
{
    private readonly IMediator _mediator;

    public ChaptersGrpcService(IMediator mediator) => _mediator = mediator;

    public override async Task<GetChaptersResponse> GetChapters(GetChaptersRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.BookId, out var bookId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid book ID."));

        var result = await _mediator.Send(new GetChaptersByBookQuery(bookId), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.Internal, result.Error!.Message));

        var response = new GetChaptersResponse();
        response.Chapters.AddRange(result.Value!.Select(MapToProto));
        return response;
    }

    public override async Task<ChapterResponse> GetChapter(GetChapterRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid chapter ID."));

        var result = await _mediator.Send(new GetChapterByIdQuery(id), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.NotFound, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    public override async Task<ChapterResponse> CreateChapter(CreateChapterRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.BookId, out var bookId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid book ID."));

        var result = await _mediator.Send(new CreateChapterCommand(bookId, request.Title, request.Order), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.Internal, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    public override async Task<ChapterResponse> UpdateChapter(UpdateChapterRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid chapter ID."));

        var result = await _mediator.Send(new UpdateChapterCommand(id, request.Title, request.Content, request.Order), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.NotFound, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    public override async Task<GetTimelineResponse> GetTimeline(GetTimelineRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ChapterId, out var chapterId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid chapter ID."));

        var result = await _mediator.Send(new GetChapterTimelineQuery(chapterId), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.Internal, result.Error!.Message));

        var response = new GetTimelineResponse();
        response.Snapshots.AddRange(result.Value!.Select(MapSnapshotToProto));
        return response;
    }

    public override async Task<SnapshotResponse> CreateSnapshot(CreateSnapshotRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ChapterId, out var chapterId) || !Guid.TryParse(request.AuthorId, out var authorId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid IDs."));

        var result = await _mediator.Send(
            new CreateSnapshotCommand(chapterId, request.Content, request.Message, authorId),
            context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.Internal, result.Error!.Message));

        return MapSnapshotToProto(result.Value!);
    }

    public override async Task<ChapterResponse> RestoreToSnapshot(RestoreToSnapshotRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ChapterId, out var chapterId) || !Guid.TryParse(request.SnapshotId, out var snapshotId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid IDs."));

        var result = await _mediator.Send(new RestoreToSnapshotCommand(chapterId, snapshotId), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.NotFound, result.Error!.Message));

        return MapToProto(result.Value!);
    }

    private static ChapterResponse MapToProto(ChapterDto dto) => new()
    {
        Id = dto.Id.ToString(),
        BookId = dto.BookId.ToString(),
        Title = dto.Title,
        Order = dto.Order,
        CurrentContent = dto.CurrentContent,
        CreatedAt = dto.CreatedAt.ToString("O"),
        UpdatedAt = dto.UpdatedAt.ToString("O")
    };

    private static SnapshotResponse MapSnapshotToProto(SnapshotDto dto) => new()
    {
        Id = dto.Id.ToString(),
        ChapterId = dto.ChapterId.ToString(),
        Content = dto.Content,
        Message = dto.SnapshotMessage,
        CreatedAt = dto.CreatedAt.ToString("O"),
        AuthorId = dto.AuthorId.ToString(),
        ParentSnapshotId = dto.ParentSnapshotId?.ToString() ?? string.Empty
    };
}
