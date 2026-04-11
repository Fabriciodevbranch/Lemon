using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using MediatR;

namespace LemonWriter.Application.Drafts.Queries;

public record GetDraftsByChapterQuery(Guid ChapterId) : IRequest<Result<IEnumerable<DraftDto>>>;

public class GetDraftsByChapterQueryHandler : IRequestHandler<GetDraftsByChapterQuery, Result<IEnumerable<DraftDto>>>
{
    private readonly IDraftRepository _draftRepository;

    public GetDraftsByChapterQueryHandler(IDraftRepository draftRepository) => _draftRepository = draftRepository;

    public async Task<Result<IEnumerable<DraftDto>>> Handle(GetDraftsByChapterQuery request, CancellationToken cancellationToken)
    {
        var drafts = await _draftRepository.GetByChapterIdAsync(request.ChapterId, cancellationToken);
        var dtos = drafts.Select(d => new DraftDto(
            d.Id, d.ChapterId, d.Title, d.Content, d.CreatedAt, d.UpdatedAt, d.IsPublished, d.PublishedAt, d.PublishedSnapshotId));
        return Result<IEnumerable<DraftDto>>.Success(dtos);
    }
}

public record GetDraftByIdQuery(Guid Id) : IRequest<Result<DraftDto>>;

public class GetDraftByIdQueryHandler : IRequestHandler<GetDraftByIdQuery, Result<DraftDto>>
{
    private readonly IDraftRepository _draftRepository;

    public GetDraftByIdQueryHandler(IDraftRepository draftRepository) => _draftRepository = draftRepository;

    public async Task<Result<DraftDto>> Handle(GetDraftByIdQuery request, CancellationToken cancellationToken)
    {
        var draft = await _draftRepository.GetByIdAsync(request.Id, cancellationToken);
        if (draft is null)
            return Result<DraftDto>.Failure(Error.NotFound);

        return Result<DraftDto>.Success(new DraftDto(
            draft.Id, draft.ChapterId, draft.Title, draft.Content,
            draft.CreatedAt, draft.UpdatedAt, draft.IsPublished,
            draft.PublishedAt, draft.PublishedSnapshotId));
    }
}
