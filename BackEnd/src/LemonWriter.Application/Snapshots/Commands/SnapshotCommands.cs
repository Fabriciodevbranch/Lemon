using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Snapshots.Commands;

public record CreateSnapshotCommand(
    Guid ChapterId,
    string Content,
    string SnapshotMessage,
    Guid AuthorId) : IRequest<Result<SnapshotDto>>;

public class CreateSnapshotCommandValidator : AbstractValidator<CreateSnapshotCommand>
{
    public CreateSnapshotCommandValidator()
    {
        RuleFor(x => x.ChapterId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.SnapshotMessage).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.AuthorId).NotEmpty();
    }
}

public class CreateSnapshotCommandHandler : IRequestHandler<CreateSnapshotCommand, Result<SnapshotDto>>
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public CreateSnapshotCommandHandler(
        ISnapshotRepository snapshotRepository,
        IChapterRepository chapterRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _snapshotRepository = snapshotRepository;
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<SnapshotDto>> Handle(CreateSnapshotCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.ChapterId, cancellationToken);
        if (chapter is null)
            return Result<SnapshotDto>.Failure(Error.NotFound);

        var latestSnapshot = await _snapshotRepository.GetLatestByChapterIdAsync(request.ChapterId, cancellationToken);
        var snapshot = Snapshot.Create(
            request.ChapterId,
            request.Content,
            request.SnapshotMessage,
            request.AuthorId,
            latestSnapshot?.Id);

        chapter.RestoreContent(request.Content);
        await _snapshotRepository.AddAsync(snapshot, cancellationToken);
        await _chapterRepository.UpdateAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var evt in snapshot.DomainEvents)
            await _eventBus.PublishAsync(evt, cancellationToken);
        snapshot.ClearDomainEvents();

        return Result<SnapshotDto>.Success(new SnapshotDto(
            snapshot.Id, snapshot.ChapterId, snapshot.Content,
            snapshot.SnapshotMessage, snapshot.CreatedAt, snapshot.AuthorId, snapshot.ParentSnapshotId));
    }
}

public record RestoreToSnapshotCommand(Guid ChapterId, Guid SnapshotId) : IRequest<Result<ChapterDto>>;

public class RestoreToSnapshotCommandHandler : IRequestHandler<RestoreToSnapshotCommand, Result<ChapterDto>>
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreToSnapshotCommandHandler(
        ISnapshotRepository snapshotRepository,
        IChapterRepository chapterRepository,
        IUnitOfWork unitOfWork)
    {
        _snapshotRepository = snapshotRepository;
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChapterDto>> Handle(RestoreToSnapshotCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.ChapterId, cancellationToken);
        if (chapter is null)
            return Result<ChapterDto>.Failure(Error.NotFound);

        var snapshot = await _snapshotRepository.GetByIdAsync(request.SnapshotId, cancellationToken);
        if (snapshot is null || snapshot.ChapterId != request.ChapterId)
            return Result<ChapterDto>.Failure(Error.NotFound);

        chapter.RestoreContent(snapshot.Content);
        await _chapterRepository.UpdateAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChapterDto>.Success(new ChapterDto(
            chapter.Id, chapter.BookId, chapter.Title, chapter.Order,
            chapter.CurrentContent, chapter.CreatedAt, chapter.UpdatedAt));
    }
}

public record GrabContentFromSnapshotCommand(Guid TargetChapterId, Guid SnapshotId) : IRequest<Result<ChapterDto>>;

public class GrabContentFromSnapshotCommandHandler : IRequestHandler<GrabContentFromSnapshotCommand, Result<ChapterDto>>
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GrabContentFromSnapshotCommandHandler(
        ISnapshotRepository snapshotRepository,
        IChapterRepository chapterRepository,
        IUnitOfWork unitOfWork)
    {
        _snapshotRepository = snapshotRepository;
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChapterDto>> Handle(GrabContentFromSnapshotCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.TargetChapterId, cancellationToken);
        if (chapter is null)
            return Result<ChapterDto>.Failure(Error.NotFound);

        var snapshot = await _snapshotRepository.GetByIdAsync(request.SnapshotId, cancellationToken);
        if (snapshot is null)
            return Result<ChapterDto>.Failure(Error.NotFound);

        // Append the snapshot content to the current chapter content
        var combined = chapter.CurrentContent + "\n\n" + snapshot.Content;
        chapter.Update(chapter.Title, combined.Trim(), chapter.Order);
        await _chapterRepository.UpdateAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChapterDto>.Success(new ChapterDto(
            chapter.Id, chapter.BookId, chapter.Title, chapter.Order,
            chapter.CurrentContent, chapter.CreatedAt, chapter.UpdatedAt));
    }
}
