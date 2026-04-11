using FluentValidation;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using MediatR;

namespace LemonWriter.Application.Export.Commands;

public record ExportBookCommand(Guid BookId, string Format) : IRequest<Result<byte[]>>;

public class ExportBookCommandValidator : AbstractValidator<ExportBookCommand>
{
    public ExportBookCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Format).NotEmpty().Must(f => f == "epub" || f == "pdf")
            .WithMessage("Format must be 'epub' or 'pdf'.");
    }
}

public class ExportBookCommandHandler : IRequestHandler<ExportBookCommand, Result<byte[]>>
{
    private readonly IExportService _exportService;
    private readonly IBookRepository _bookRepository;

    public ExportBookCommandHandler(IExportService exportService, IBookRepository bookRepository)
    {
        _exportService = exportService;
        _bookRepository = bookRepository;
    }

    public async Task<Result<byte[]>> Handle(ExportBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null)
            return Result<byte[]>.Failure(Error.NotFound);

        var data = await _exportService.ExportBookAsync(request.BookId, request.Format, cancellationToken);
        return Result<byte[]>.Success(data);
    }
}
