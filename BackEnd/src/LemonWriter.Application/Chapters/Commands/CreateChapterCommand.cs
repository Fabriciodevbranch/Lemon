using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Chapters.Commands;

public record CreateChapterCommand(Guid BookId, string Title, int Order) : IRequest<Result<ChapterDto>>;

public class CreateChapterCommandValidator : AbstractValidator<CreateChapterCommand>
{
    public CreateChapterCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}

public class CreateChapterCommandHandler : IRequestHandler<CreateChapterCommand, Result<ChapterDto>>
{
    private readonly IChapterRepository _chapterRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public CreateChapterCommandHandler(
        IChapterRepository chapterRepository,
        IBookRepository bookRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _chapterRepository = chapterRepository;
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<ChapterDto>> Handle(CreateChapterCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null)
            return Result<ChapterDto>.Failure(Error.NotFound);

        var chapter = Chapter.Create(request.BookId, request.Title, request.Order);
        await _chapterRepository.AddAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var evt in chapter.DomainEvents)
            await _eventBus.PublishAsync(evt, cancellationToken);
        chapter.ClearDomainEvents();

        return Result<ChapterDto>.Success(new ChapterDto(
            chapter.Id, chapter.BookId, chapter.Title, chapter.Order,
            chapter.CurrentContent, chapter.CreatedAt, chapter.UpdatedAt));
    }
}
