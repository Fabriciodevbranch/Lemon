using System.IO.Compression;
using System.Net;
using System.Text;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LemonWriter.Infrastructure.Services.Export;

internal sealed record ExportBookData(string Title, string Description, string Author, string? Cover, bool IncludeBranding,
    IReadOnlyList<ExportChapterData> Chapters);
internal sealed record ExportChapterData(string Title, string Content);

internal static class ExportDataLoader
{
    public static async Task<ExportBookData> Load(LemonDbContext db, Guid bookId, CancellationToken ct)
    {
        var book = await db.Books.AsNoTracking().FirstAsync(x => x.Id == bookId, ct);
        var chapters = await db.Chapters.AsNoTracking().Where(x => x.BookId == bookId)
            .OrderBy(x => x.Order).Select(x => new ExportChapterData(x.Title, x.CurrentContent)).ToListAsync(ct);
        var includeBranding = await db.Users.AsNoTracking().Where(x => x.Id == book.AuthorId)
            .Select(x => (bool?)x.IncludeExportBranding).FirstOrDefaultAsync(ct) ?? true;
        return new(book.Title, book.Description, book.Metadata.AuthorName, book.Metadata.CoverImageUrl, includeBranding, chapters);
    }

    public static (byte[] Bytes, string Mime)? DecodeImage(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return null;
        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return null;
        try
        {
            var header = dataUrl[..comma];
            var mime = header[5..header.IndexOf(';')];
            return (Convert.FromBase64String(dataUrl[(comma + 1)..]), mime);
        }
        catch { return null; }
    }
}

public class EpubExportService : IExportService
{
    private readonly LemonDbContext _db;
    public EpubExportService(LemonDbContext db) => _db = db;

    public async Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default)
    {
        if (!format.Equals("epub", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("EpubExportService only supports EPUB.");
        var book = await ExportDataLoader.Load(_db, bookId, cancellationToken);
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(zip, "mimetype", "application/epub+zip", CompressionLevel.NoCompression);
            Write(zip, "META-INF/container.xml", """<?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/></rootfiles></container>""");

            var image = ExportDataLoader.DecodeImage(book.Cover);
            var imageExtension = image?.Mime switch { "image/png" => "png", "image/gif" => "gif", "image/webp" => "webp", _ => "jpg" };
            var coverManifest = image is null ? "" : $"<item id=\"cover-image\" href=\"cover.{imageExtension}\" media-type=\"{image.Value.Mime}\" properties=\"cover-image\"/>";
            var chapterManifest = string.Join("", book.Chapters.Select((_, i) => $"<item id=\"chapter{i + 1}\" href=\"chapter{i + 1}.xhtml\" media-type=\"application/xhtml+xml\"/>"));
            var chapterSpine = string.Join("", book.Chapters.Select((_, i) => $"<itemref idref=\"chapter{i + 1}\"/>"));
            var identifier = $"urn:uuid:{bookId}";
            Write(zip, "OEBPS/content.opf", $"""<?xml version="1.0" encoding="UTF-8"?><package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="book-id"><metadata xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:identifier id="book-id">{identifier}</dc:identifier><dc:title>{E(book.Title)}</dc:title><dc:creator>{E(book.Author)}</dc:creator><dc:language>en</dc:language><meta property="dcterms:modified">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta></metadata><manifest><item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/><item id="cover" href="cover.xhtml" media-type="application/xhtml+xml"/>{coverManifest}{chapterManifest}</manifest><spine><itemref idref="cover"/>{chapterSpine}</spine></package>""");

            var coverImage = image is null ? "" : $"<img src=\"cover.{imageExtension}\" alt=\"Book cover\"/>";
            Write(zip, "OEBPS/cover.xhtml", Page(book.Title, $"<section class=\"cover\">{coverImage}<h1>{E(book.Title)}</h1><p>{E(book.Author)}</p><p>{E(book.Description)}</p></section>"));
            var navItems = book.Chapters.Count == 0 ? "<li><a href=\"cover.xhtml\">Cover</a></li>" : string.Join("", book.Chapters.Select((c, i) => $"<li><a href=\"chapter{i + 1}.xhtml\">{E(c.Title)}</a></li>"));
            Write(zip, "OEBPS/nav.xhtml", Page("Contents", $"<nav epub:type=\"toc\" xmlns:epub=\"http://www.idpf.org/2007/ops\"><h1>Contents</h1><ol>{navItems}</ol></nav>"));
            for (var i = 0; i < book.Chapters.Count; i++)
            {
                var chapter = book.Chapters[i];
                Write(zip, $"OEBPS/chapter{i + 1}.xhtml", Page(chapter.Title, $"<h1>{E(chapter.Title)}</h1><p>{E(chapter.Content).Replace("\n", "</p><p>")}</p>"));
            }
            if (image is not null) WriteBytes(zip, $"OEBPS/cover.{imageExtension}", image.Value.Bytes);
        }
        return output.ToArray();
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Page(string title, string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>" +
        E(title) + "</title><style>body{font-family:serif;margin:8%;line-height:1.6}.cover{text-align:center}" +
        ".cover img{max-height:70vh;max-width:100%}h1{page-break-before:always}</style></head><body>" + body + "</body></html>";
    private static void Write(ZipArchive zip, string path, string value, CompressionLevel compression = CompressionLevel.Optimal)
    { var entry = zip.CreateEntry(path, compression); using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(value); }
    private static void WriteBytes(ZipArchive zip, string path, byte[] value)
    { var entry = zip.CreateEntry(path, CompressionLevel.Optimal); using var stream = entry.Open(); stream.Write(value); }
}

public class PdfExportService : IExportService
{
    private readonly LemonDbContext _db;
    static PdfExportService() => QuestPDF.Settings.License = LicenseType.Community;
    public PdfExportService(LemonDbContext db) => _db = db;

    public async Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default)
    {
        if (!format.Equals("pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("PdfExportService only supports PDF.");
        var book = await ExportDataLoader.Load(_db, bookId, cancellationToken);
        var image = ExportDataLoader.DecodeImage(book.Cover);
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A5); page.Margin(34); page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Georgia));
                page.Content().Column(column =>
                {
                    if (image is not null) column.Item().MaxHeight(440).AlignCenter().Image(image.Value.Bytes).FitArea();
                    column.Item().PaddingTop(20).AlignCenter().Text(book.Title).FontSize(28).Bold();
                    column.Item().PaddingTop(8).AlignCenter().Text(book.Author).FontSize(13);
                    if (!string.IsNullOrWhiteSpace(book.Description)) column.Item().PaddingTop(20).Text(book.Description).Italic();
                    if (book.Chapters.Count == 0) column.Item().PaddingTop(28).AlignCenter().Text("This book does not have chapters yet.").FontColor(Colors.Grey.Medium);
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    if (book.IncludeBranding) text.Span("Generated by Lemon Writer · ");
                    text.CurrentPageNumber();
                });
            });
            foreach (var chapter in book.Chapters)
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A5); page.Margin(44); page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Georgia));
                    page.Header().Text(chapter.Title).FontSize(9).FontColor(Colors.Grey.Medium);
                    page.Content().Column(column => { column.Spacing(14); column.Item().Text(chapter.Title).FontSize(24).Bold(); column.Item().Text(chapter.Content); });
                    page.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
                });
            }
        }).GeneratePdf();
    }
}

public class CompositeExportService : IExportService
{
    private readonly EpubExportService _epub;
    private readonly PdfExportService _pdf;
    public CompositeExportService(EpubExportService epub, PdfExportService pdf) { _epub = epub; _pdf = pdf; }
    public Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default) => format.ToLowerInvariant() switch
    {
        "epub" => _epub.ExportBookAsync(bookId, format, cancellationToken),
        "pdf" => _pdf.ExportBookAsync(bookId, format, cancellationToken),
        _ => throw new InvalidOperationException($"Unsupported export format: {format}")
    };
}
