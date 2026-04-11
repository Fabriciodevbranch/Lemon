using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Chapters.Commands;

public record UpdateChapterCommand(Guid Id, string Title, string Content, int Order) : IRequest<Result<ChapterDto>>;

public class UpdateChapterCommandValidator : AbstractValidator<UpdateChapterCommand>
{
    public UpdateChapterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}

public class UpdateChapterCommandHandler : IRequestHandler<UpdateChapterCommand, Result<ChapterDto>>
{
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateChapterCommandHandler(IChapterRepository chapterRepository, IUnitOfWork unitOfWork)
    {
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChapterDto>> Handle(UpdateChapterCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.Id, cancellationToken);
        if (chapter is null)
            return Result<ChapterDto>.Failure(Error.NotFound);

        chapter.Update(request.Title, request.Content, request.Order);
        await _chapterRepository.UpdateAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChapterDto>.Success(new ChapterDto(
            chapter.Id, chapter.BookId, chapter.Title, chapter.Order,
            chapter.CurrentContent, chapter.CreatedAt, chapter.UpdatedAt));
    }
}

public record DeleteChapterCommand(Guid Id) : IRequest<Result>;

public class DeleteChapterCommandValidator : AbstractValidator<DeleteChapterCommand>
{
    public DeleteChapterCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class DeleteChapterCommandHandler : IRequestHandler<DeleteChapterCommand, Result>
{
    private readonly IChapterRepository _chapterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteChapterCommandHandler(IChapterRepository chapterRepository, IUnitOfWork unitOfWork)
    {
        _chapterRepository = chapterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteChapterCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.Id, cancellationToken);
        if (chapter is null)
            return Result.Failure(Error.NotFound);

        await _chapterRepository.DeleteAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
