using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Books.Commands;

public record CreateBookCommand(
    string Title,
    string Description,
    Guid AuthorId,
    string AuthorName,
    string? ISBN = null,
    string? INBR = null,
    string? CoverImageUrl = null,
    bool IsSeries = false,
    int? SeriesVolume = null,
    string? SeriesName = null) : IRequest<Result<BookDto>>;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AuthorId).NotEmpty();
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(200);
    }
}

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, Result<BookDto>>
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public CreateBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork, IEventBus eventBus)
    {
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<BookDto>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var metadata = BookMetadata.Create(
            request.AuthorName,
            request.ISBN,
            request.INBR,
            request.CoverImageUrl,
            request.Description,
            request.IsSeries,
            request.SeriesVolume,
            request.SeriesName);

        var book = Book.Create(request.Title, request.Description, request.AuthorId, metadata);
        await _bookRepository.AddAsync(book, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var evt in book.DomainEvents)
            await _eventBus.PublishAsync(evt, cancellationToken);
        book.ClearDomainEvents();

        return Result<BookDto>.Success(MapToDto(book));
    }

    private static BookDto MapToDto(Book book) => new(
        book.Id, book.Title, book.Description, book.AuthorId,
        book.Metadata.ISBN, book.Metadata.INBR, book.Metadata.AuthorName,
        book.Metadata.CoverImageUrl, book.Metadata.IsSeries,
        book.Metadata.SeriesVolume, book.Metadata.SeriesName,
        book.CreatedAt, book.UpdatedAt);
}
