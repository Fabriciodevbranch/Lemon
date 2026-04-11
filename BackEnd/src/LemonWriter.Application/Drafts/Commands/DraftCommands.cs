using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Drafts.Commands;

public record CreateDraftCommand(Guid ChapterId, string Title, string InitialContent = "") : IRequest<Result<DraftDto>>;

public class CreateDraftCommandValidator : AbstractValidator<CreateDraftCommand>
{
    public CreateDraftCommandValidator()
    {
        RuleFor(x => x.ChapterId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public class CreateDraftCommandHandler : IRequestHandler<CreateDraftCommand, Result<DraftDto>>
{
    private readonly IDraftRepository _draftRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDraftCommandHandler(
        IDraftRepository draftRepository,
        IChapterRepository chapterRepository,
        IUnitOfWork unitOfWork)
    {
        _draftRepository = draftRepository;
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DraftDto>> Handle(CreateDraftCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.ChapterId, cancellationToken);
        if (chapter is null)
            return Result<DraftDto>.Failure(Error.NotFound);

        var draft = Draft.Create(request.ChapterId, request.Title, request.InitialContent);
        await _draftRepository.AddAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DraftDto>.Success(MapToDto(draft));
    }

    private static DraftDto MapToDto(Draft draft) => new(
        draft.Id, draft.ChapterId, draft.Title, draft.Content,
        draft.CreatedAt, draft.UpdatedAt, draft.IsPublished,
        draft.PublishedAt, draft.PublishedSnapshotId);
}

public record UpdateDraftCommand(Guid Id, string Title, string Content) : IRequest<Result<DraftDto>>;

public class UpdateDraftCommandValidator : AbstractValidator<UpdateDraftCommand>
{
    public UpdateDraftCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public class UpdateDraftCommandHandler : IRequestHandler<UpdateDraftCommand, Result<DraftDto>>
{
    private readonly IDraftRepository _draftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDraftCommandHandler(IDraftRepository draftRepository, IUnitOfWork unitOfWork)
    {
        _draftRepository = draftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DraftDto>> Handle(UpdateDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (draft is null)
            return Result<DraftDto>.Failure(Error.NotFound);

        draft.Update(request.Title, request.Content);
        await _draftRepository.UpdateAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DraftDto>.Success(new DraftDto(
            draft.Id, draft.ChapterId, draft.Title, draft.Content,
            draft.CreatedAt, draft.UpdatedAt, draft.IsPublished,
            draft.PublishedAt, draft.PublishedSnapshotId));
    }
}

public record PublishDraftCommand(Guid Id, string PublishMessage, Guid AuthorId) : IRequest<Result<DraftDto>>;

public class PublishDraftCommandValidator : AbstractValidator<PublishDraftCommand>
{
    public PublishDraftCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PublishMessage).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.AuthorId).NotEmpty();
    }
}

public class PublishDraftCommandHandler : IRequestHandler<PublishDraftCommand, Result<DraftDto>>
{
    private readonly IDraftRepository _draftRepository;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public PublishDraftCommandHandler(
        IDraftRepository draftRepository,
        ISnapshotRepository snapshotRepository,
        IChapterRepository chapterRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _draftRepository = draftRepository;
        _snapshotRepository = snapshotRepository;
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<DraftDto>> Handle(PublishDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (draft is null)
            return Result<DraftDto>.Failure(Error.NotFound);

        if (draft.IsPublished)
            return Result<DraftDto>.Failure(Error.Custom("ALREADY_PUBLISHED", "This draft has already been published."));

        var chapter = await _chapterRepository.GetByIdAsync(draft.ChapterId, cancellationToken);
        if (chapter is null)
            return Result<DraftDto>.Failure(Error.NotFound);

        var latestSnapshot = await _snapshotRepository.GetLatestByChapterIdAsync(draft.ChapterId, cancellationToken);
        var snapshot = Snapshot.Create(
            draft.ChapterId, draft.Content, request.PublishMessage, request.AuthorId, latestSnapshot?.Id);

        chapter.RestoreContent(draft.Content);

        await _snapshotRepository.AddAsync(snapshot, cancellationToken);
        await _chapterRepository.UpdateAsync(chapter, cancellationToken);

        draft.Publish(snapshot.Id);
        await _draftRepository.UpdateAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var evt in draft.DomainEvents)
            await _eventBus.PublishAsync(evt, cancellationToken);
        draft.ClearDomainEvents();

        return Result<DraftDto>.Success(new DraftDto(
            draft.Id, draft.ChapterId, draft.Title, draft.Content,
            draft.CreatedAt, draft.UpdatedAt, draft.IsPublished,
            draft.PublishedAt, draft.PublishedSnapshotId));
    }
}

public record DeleteDraftCommand(Guid Id) : IRequest<Result>;

public class DeleteDraftCommandValidator : AbstractValidator<DeleteDraftCommand>
{
    public DeleteDraftCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class DeleteDraftCommandHandler : IRequestHandler<DeleteDraftCommand, Result>
{
    private readonly IDraftRepository _draftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDraftCommandHandler(IDraftRepository draftRepository, IUnitOfWork unitOfWork)
    {
        _draftRepository = draftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (draft is null)
            return Result.Failure(Error.NotFound);

        await _draftRepository.DeleteAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
