using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using MediatR;

namespace LemonWriter.Application.Books.Queries;

public record GetBooksQuery(Guid AuthorId) : IRequest<Result<IEnumerable<BookDto>>>;

public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, Result<IEnumerable<BookDto>>>
{
    private readonly IBookRepository _bookRepository;

    public GetBooksQueryHandler(IBookRepository bookRepository) => _bookRepository = bookRepository;

    public async Task<Result<IEnumerable<BookDto>>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await _bookRepository.GetByAuthorIdAsync(request.AuthorId, cancellationToken);
        var dtos = books.Select(b => new BookDto(
            b.Id, b.Title, b.Description, b.AuthorId,
            b.Metadata.ISBN, b.Metadata.INBR, b.Metadata.AuthorName,
            b.Metadata.CoverImageUrl, b.Metadata.IsSeries,
            b.Metadata.SeriesVolume, b.Metadata.SeriesName,
            b.CreatedAt, b.UpdatedAt));
        return Result<IEnumerable<BookDto>>.Success(dtos);
    }
}

public record GetBookByIdQuery(Guid Id) : IRequest<Result<BookDto>>;

public class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, Result<BookDto>>
{
    private readonly IBookRepository _bookRepository;

    public GetBookByIdQueryHandler(IBookRepository bookRepository) => _bookRepository = bookRepository;

    public async Task<Result<BookDto>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);
        if (book is null)
            return Result<BookDto>.Failure(Error.NotFound);

        return Result<BookDto>.Success(new BookDto(
            book.Id, book.Title, book.Description, book.AuthorId,
            book.Metadata.ISBN, book.Metadata.INBR, book.Metadata.AuthorName,
            book.Metadata.CoverImageUrl, book.Metadata.IsSeries,
            book.Metadata.SeriesVolume, book.Metadata.SeriesName,
            book.CreatedAt, book.UpdatedAt));
    }
}
