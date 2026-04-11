using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using MediatR;

namespace LemonWriter.Application.Chapters.Queries;

public record GetChaptersByBookQuery(Guid BookId) : IRequest<Result<IEnumerable<ChapterDto>>>;

public class GetChaptersByBookQueryHandler : IRequestHandler<GetChaptersByBookQuery, Result<IEnumerable<ChapterDto>>>
{
    private readonly IChapterRepository _chapterRepository;

    public GetChaptersByBookQueryHandler(IChapterRepository chapterRepository) => _chapterRepository = chapterRepository;

    public async Task<Result<IEnumerable<ChapterDto>>> Handle(GetChaptersByBookQuery request, CancellationToken cancellationToken)
    {
        var chapters = await _chapterRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        var dtos = chapters.OrderBy(c => c.Order).Select(c => new ChapterDto(
            c.Id, c.BookId, c.Title, c.Order, c.CurrentContent, c.CreatedAt, c.UpdatedAt));
        return Result<IEnumerable<ChapterDto>>.Success(dtos);
    }
}

public record GetChapterByIdQuery(Guid Id) : IRequest<Result<ChapterDto>>;

public class GetChapterByIdQueryHandler : IRequestHandler<GetChapterByIdQuery, Result<ChapterDto>>
{
    private readonly IChapterRepository _chapterRepository;

    public GetChapterByIdQueryHandler(IChapterRepository chapterRepository) => _chapterRepository = chapterRepository;

    public async Task<Result<ChapterDto>> Handle(GetChapterByIdQuery request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.Id, cancellationToken);
        if (chapter is null)
            return Result<ChapterDto>.Failure(Error.NotFound);

        return Result<ChapterDto>.Success(new ChapterDto(
            chapter.Id, chapter.BookId, chapter.Title, chapter.Order,
            chapter.CurrentContent, chapter.CreatedAt, chapter.UpdatedAt));
    }
}

public record GetChapterTimelineQuery(Guid ChapterId) : IRequest<Result<IEnumerable<SnapshotDto>>>;

public class GetChapterTimelineQueryHandler : IRequestHandler<GetChapterTimelineQuery, Result<IEnumerable<SnapshotDto>>>
{
    private readonly ISnapshotRepository _snapshotRepository;

    public GetChapterTimelineQueryHandler(ISnapshotRepository snapshotRepository) => _snapshotRepository = snapshotRepository;

    public async Task<Result<IEnumerable<SnapshotDto>>> Handle(GetChapterTimelineQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await _snapshotRepository.GetByChapterIdAsync(request.ChapterId, cancellationToken);
        var dtos = snapshots.OrderByDescending(s => s.CreatedAt).Select(s => new SnapshotDto(
            s.Id, s.ChapterId, s.Content, s.SnapshotMessage, s.CreatedAt, s.AuthorId, s.ParentSnapshotId));
        return Result<IEnumerable<SnapshotDto>>.Success(dtos);
    }
}
