namespace LemonWriter.Domain.Entities;

public class BookMetadata
{
    public string? ISBN { get; private set; }
    public string? INBR { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string? CoverImageUrl { get; private set; }
    public string? Description { get; private set; }
    public bool IsSeries { get; private set; }
    public int? SeriesVolume { get; private set; }
    public string? SeriesName { get; private set; }

    private BookMetadata() { }

    public static BookMetadata Create(
        string authorName,
        string? isbn = null,
        string? inbr = null,
        string? coverImageUrl = null,
        string? description = null,
        bool isSeries = false,
        int? seriesVolume = null,
        string? seriesName = null)
    {
        return new BookMetadata
        {
            AuthorName = authorName,
            ISBN = isbn,
            INBR = inbr,
            CoverImageUrl = coverImageUrl,
            Description = description,
            IsSeries = isSeries,
            SeriesVolume = seriesVolume,
            SeriesName = seriesName
        };
    }

    public BookMetadata WithUpdates(
        string? authorName = null,
        string? isbn = null,
        string? inbr = null,
        string? coverImageUrl = null,
        string? description = null,
        bool? isSeries = null,
        int? seriesVolume = null,
        string? seriesName = null)
    {
        return new BookMetadata
        {
            AuthorName = authorName ?? AuthorName,
            ISBN = isbn ?? ISBN,
            INBR = inbr ?? INBR,
            CoverImageUrl = coverImageUrl ?? CoverImageUrl,
            Description = description ?? Description,
            IsSeries = isSeries ?? IsSeries,
            SeriesVolume = seriesVolume ?? SeriesVolume,
            SeriesName = seriesName ?? SeriesName
        };
    }
}
