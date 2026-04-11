using LemonWriter.Application.Common.Interfaces;

namespace LemonWriter.Infrastructure.Services.Export;

public class EpubExportService : IExportService
{
    public Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default)
    {
        if (format.ToLowerInvariant() != "epub")
            throw new InvalidOperationException("EpubExportService only supports epub format.");

        // Stub: return placeholder epub bytes
        var placeholder = System.Text.Encoding.UTF8.GetBytes($"[EPUB PLACEHOLDER FOR BOOK {bookId}]");
        return Task.FromResult(placeholder);
    }
}

public class PdfExportService : IExportService
{
    public Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default)
    {
        if (format.ToLowerInvariant() != "pdf")
            throw new InvalidOperationException("PdfExportService only supports pdf format.");

        // Stub: return placeholder pdf bytes
        var placeholder = System.Text.Encoding.UTF8.GetBytes($"[PDF PLACEHOLDER FOR BOOK {bookId}]");
        return Task.FromResult(placeholder);
    }
}

public class CompositeExportService : IExportService
{
    private readonly EpubExportService _epubService;
    private readonly PdfExportService _pdfService;

    public CompositeExportService(EpubExportService epubService, PdfExportService pdfService)
    {
        _epubService = epubService;
        _pdfService = pdfService;
    }

    public Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default)
    {
        return format.ToLowerInvariant() switch
        {
            "epub" => _epubService.ExportBookAsync(bookId, format, cancellationToken),
            "pdf" => _pdfService.ExportBookAsync(bookId, format, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported export format: {format}")
        };
    }
}
