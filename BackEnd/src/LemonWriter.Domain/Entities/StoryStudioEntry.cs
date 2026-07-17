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

    private StoryStudioEntry() { }

    public static StoryStudioEntry Create(Guid bookId, string type, string name, string? summary,
        string? details, string? motivation, string? plot, string? imageData,
        string? eventDate = null, string? impact = null, string? characterIds = null, string? objectIds = null, string? placeIds = null,
        int? goalTarget = null, int goalProgress = 0) => new()
    {
        Id = Guid.NewGuid(), BookId = bookId, Type = type, Name = name.Trim(),
        Summary = summary?.Trim() ?? string.Empty, Details = details?.Trim() ?? string.Empty,
        Motivation = motivation?.Trim(), Plot = plot?.Trim(), ImageData = imageData,
        EventDate = eventDate?.Trim(), Impact = impact?.Trim(), RelatedCharacterIds = characterIds,
        RelatedObjectIds = objectIds, RelatedPlaceIds = placeIds,
        GoalTarget = goalTarget is > 0 ? goalTarget : null, GoalProgress = Math.Max(0, goalProgress),
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    public void SetSortOrder(int order) => SortOrder = Math.Max(0, order);
    public void SetGoalProgress(int progress) => GoalProgress = Math.Clamp(progress, 0, GoalTarget ?? int.MaxValue);
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
