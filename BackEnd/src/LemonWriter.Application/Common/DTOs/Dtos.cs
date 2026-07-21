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

public record LibraryBookStatsDto(int ChapterCount, int WordCount)
{
    public int Progress => Math.Min(100, WordCount / 800);
}

public record StudioEntryDto(Guid Id, Guid BookId, string Type, string Name, string Summary, string Details,
    string? Motivation, string? Plot, string? ImageData, DateTime CreatedAt, DateTime UpdatedAt, int SortOrder,
    string? EventDate, string? Impact, string? RelatedCharacterIds, string? RelatedObjectIds, string? RelatedPlaceIds,
    int? GoalTarget, int GoalProgress, Guid? CollectionId = null, string? CollectionName = null,
    string? StoryRole = null, string? CharacterStatus = null, string? Age = null, string? Pronouns = null,
    string? Aliases = null, Guid? PortraitMediaId = null, string? ExternalGoal = null, string? InternalNeed = null,
    string? Fear = null, string? Secret = null, string? InternalConflict = null, string? ExternalConflict = null,
    string? NarrativeFunction = null, string? ArcSummary = null, string? StartingState = null,
    string? TurningPoint = null, string? EndingState = null, string? Notes = null);

public record CreateStudioEntryDto(string Name, string? Summary, string? Details, string? Motivation, string? Plot,
    string? Image, string? EventDate, string? Impact, string? CharacterIds, string? ObjectIds, string? PlaceIds,
    int? GoalTarget, int? GoalProgress, string? StoryRole = null, Guid? PortraitMediaId = null);

public record CreateGalleryBatchDto(string? CollectionName, IReadOnlyList<CreateStudioEntryDto> Items);
public record UpdateStudioEntryMetadataDto(string Name, string? Summary, string? Details);
public record UpdateCharacterProfileDto(string Name, string? Summary, string? StoryRole, string? CharacterStatus,
    string? Age, string? Pronouns, string? Aliases, Guid? PortraitMediaId, string? ExternalGoal, string? InternalNeed,
    string? Fear, string? Secret, string? InternalConflict, string? ExternalConflict, string? NarrativeFunction,
    string? ArcSummary, string? StartingState, string? TurningPoint, string? EndingState, string? Notes);

public record StoryRelationshipDto(Guid Id, Guid From, Guid To, string Label, string Tone);
public record CreateStoryRelationshipDto(Guid From, Guid To, string? Label, string? Tone);

public record StoryMetricsDto(bool Enabled, int Characters = 0, int Places = 0, int Objects = 0, int Gallery = 0,
    int Relationships = 0, int Completeness = 0, int StoryCompleteness = 0, int CharacterCoverage = 0,
    int RelationshipCoverage = 0, int TimelineCompleteness = 0, int OrphanCharacters = 0,
    int EmptyLocations = 0, int UnusedObjects = 0);
