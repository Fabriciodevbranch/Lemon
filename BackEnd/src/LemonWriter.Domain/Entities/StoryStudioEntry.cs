using LemonWriter.Domain.Common;

namespace LemonWriter.Domain.Entities;

public sealed class StoryStudioEntry : Entity<Guid>
{
    public Guid BookId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string? Motivation { get; private set; }
    public string? Plot { get; private set; }
    public string? ImageData { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int SortOrder { get; private set; }
    public string? EventDate { get; private set; }
    public string? Impact { get; private set; }
    public string? RelatedCharacterIds { get; private set; }
    public string? RelatedObjectIds { get; private set; }
    public string? RelatedPlaceIds { get; private set; }
    public int? GoalTarget { get; private set; }
    public int GoalProgress { get; private set; }
    public Guid? CollectionId { get; private set; }
    public string? StoryRole { get; private set; }
    public string? CharacterStatus { get; private set; }
    public string? Age { get; private set; }
    public string? Pronouns { get; private set; }
    public string? Aliases { get; private set; }
    public Guid? PortraitMediaId { get; private set; }
    public string? ExternalGoal { get; private set; }
    public string? InternalNeed { get; private set; }
    public string? Fear { get; private set; }
    public string? Secret { get; private set; }
    public string? InternalConflict { get; private set; }
    public string? ExternalConflict { get; private set; }
    public string? NarrativeFunction { get; private set; }
    public string? ArcSummary { get; private set; }
    public string? StartingState { get; private set; }
    public string? TurningPoint { get; private set; }
    public string? EndingState { get; private set; }
    public string? Notes { get; private set; }

    private StoryStudioEntry() { }

    public static StoryStudioEntry Create(Guid bookId, string type, string name, string? summary,
        string? details, string? motivation, string? plot, string? imageData,
        string? eventDate = null, string? impact = null, string? characterIds = null, string? objectIds = null, string? placeIds = null,
        int? goalTarget = null, int goalProgress = 0, Guid? collectionId = null, string? storyRole = null, Guid? portraitMediaId = null) => new()
    {
        Id = Guid.NewGuid(), BookId = bookId, Type = type, Name = name.Trim(),
        Summary = summary?.Trim() ?? string.Empty, Details = details?.Trim() ?? string.Empty,
        Motivation = motivation?.Trim(), Plot = plot?.Trim(), ImageData = imageData,
        EventDate = eventDate?.Trim(), Impact = impact?.Trim(), RelatedCharacterIds = characterIds,
        RelatedObjectIds = objectIds, RelatedPlaceIds = placeIds,
        GoalTarget = goalTarget is > 0 ? goalTarget : null, GoalProgress = Math.Max(0, goalProgress), CollectionId = collectionId,
        StoryRole = storyRole?.Trim(), PortraitMediaId = portraitMediaId,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    public void SetSortOrder(int order) => SortOrder = Math.Max(0, order);
    public void SetGoalProgress(int progress) => GoalProgress = Math.Clamp(progress, 0, GoalTarget ?? int.MaxValue);
    public void UpdateMetadata(string name, string? summary, string? details)
    {
        Name = name.Trim();
        Summary = summary?.Trim() ?? string.Empty;
        Details = details?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
    public void UpdateCharacterProfile(string name, string? summary, string? storyRole, string? status, string? age,
        string? pronouns, string? aliases, Guid? portraitMediaId, string? externalGoal, string? internalNeed,
        string? fear, string? secret, string? internalConflict, string? externalConflict, string? narrativeFunction,
        string? arcSummary, string? startingState, string? turningPoint, string? endingState, string? notes)
    {
        Name = name.Trim(); Summary = summary?.Trim() ?? string.Empty; StoryRole = storyRole?.Trim();
        CharacterStatus = status?.Trim(); Age = age?.Trim(); Pronouns = pronouns?.Trim(); Aliases = aliases?.Trim();
        PortraitMediaId = portraitMediaId; ExternalGoal = externalGoal?.Trim(); InternalNeed = internalNeed?.Trim();
        Fear = fear?.Trim(); Secret = secret?.Trim(); InternalConflict = internalConflict?.Trim(); ExternalConflict = externalConflict?.Trim();
        NarrativeFunction = narrativeFunction?.Trim(); ArcSummary = arcSummary?.Trim(); StartingState = startingState?.Trim();
        TurningPoint = turningPoint?.Trim(); EndingState = endingState?.Trim(); Notes = notes?.Trim(); UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class StoryMediaCollection : Entity<Guid>
{
    public Guid BookId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private StoryMediaCollection() { }

    public static StoryMediaCollection Create(Guid bookId, string name) => new()
    {
        Id = Guid.NewGuid(), BookId = bookId, Name = name.Trim(), CreatedAt = DateTime.UtcNow
    };
}

public sealed class StoryRelationship : Entity<Guid>
{
    public Guid BookId { get; private set; }
    public Guid FromEntryId { get; private set; }
    public Guid ToEntryId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Tone { get; private set; } = "neutral";
    public DateTime CreatedAt { get; private set; }

    private StoryRelationship() { }

    public static StoryRelationship Create(Guid bookId, Guid from, Guid to, string? label, string? tone) => new()
    {
        Id = Guid.NewGuid(), BookId = bookId, FromEntryId = from, ToEntryId = to,
        Label = string.IsNullOrWhiteSpace(label) ? "connected to" : label.Trim(),
        Tone = tone is "positive" or "negative" ? tone : "neutral", CreatedAt = DateTime.UtcNow
    };
}
