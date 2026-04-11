using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Books.Commands;

public record UpdateBookCommand(
    Guid Id,
    string Title,
    string Description,
    string AuthorName,
    string? ISBN = null,
    string? INBR = null,
    string? CoverImageUrl = null,
    bool IsSeries = false,
    int? SeriesVolume = null,
    string? SeriesName = null) : IRequest<Result<BookDto>>;

public class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(200);
    }
}

public class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, Result<BookDto>>
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BookDto>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);
        if (book is null)
            return Result<BookDto>.Failure(Error.NotFound);

        var metadata = book.Metadata.WithUpdates(
            request.AuthorName, request.ISBN, request.INBR,
            request.CoverImageUrl, request.Description,
            request.IsSeries, request.SeriesVolume, request.SeriesName);

        book.Update(request.Title, request.Description, metadata);
        await _bookRepository.UpdateAsync(book, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BookDto>.Success(new BookDto(
            book.Id, book.Title, book.Description, book.AuthorId,
            book.Metadata.ISBN, book.Metadata.INBR, book.Metadata.AuthorName,
            book.Metadata.CoverImageUrl, book.Metadata.IsSeries,
            book.Metadata.SeriesVolume, book.Metadata.SeriesName,
            book.CreatedAt, book.UpdatedAt));
    }
}
