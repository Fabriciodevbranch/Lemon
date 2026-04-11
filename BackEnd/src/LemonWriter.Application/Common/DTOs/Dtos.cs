namespace LemonWriter.Application.Common.DTOs;

public record BookDto(
    Guid Id,
    string Title,
    string Description,
    Guid AuthorId,
    string? ISBN,
    string? INBR,
    string AuthorName,
    string? CoverImageUrl,
    bool IsSeries,
    int? SeriesVolume,
    string? SeriesName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ChapterDto(
    Guid Id,
    Guid BookId,
    string Title,
    int Order,
    string CurrentContent,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SnapshotDto(
    Guid Id,
    Guid ChapterId,
    string Content,
    string SnapshotMessage,
    DateTime CreatedAt,
    Guid AuthorId,
    Guid? ParentSnapshotId);

public record DraftDto(
    Guid Id,
    Guid ChapterId,
    string Title,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsPublished,
    DateTime? PublishedAt,
    Guid? PublishedSnapshotId);

public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string? OAuthProvider,
    DateTime CreatedAt);

public record SnapshotComparisonDto(
    SnapshotDto BaseSnapshot,
    SnapshotDto CompareSnapshot,
    IEnumerable<string> Differences);
