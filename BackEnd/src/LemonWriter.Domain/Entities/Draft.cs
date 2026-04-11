using LemonWriter.Domain.Common;
using LemonWriter.Domain.Events;

namespace LemonWriter.Domain.Entities;

public class Draft : AggregateRoot<Guid>
{
    public Guid ChapterId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid? PublishedSnapshotId { get; private set; }

    private Draft() { }

    public static Draft Create(Guid chapterId, string title, string initialContent = "")
    {
        var draft = new Draft
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            Title = title,
            Content = initialContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPublished = false
        };
        return draft;
    }

    public void Update(string title, string content)
    {
        Title = title;
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish(Guid snapshotId)
    {
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        PublishedSnapshotId = snapshotId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DraftPublishedEvent(Id, ChapterId, snapshotId));
    }
}
