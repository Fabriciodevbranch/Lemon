using LemonWriter.Domain.Common;

namespace LemonWriter.Domain.Events;

public sealed class SnapshotCreatedEvent : DomainEvent
{
    public Guid SnapshotId { get; }
    public Guid ChapterId { get; }
    public Guid AuthorId { get; }

    public SnapshotCreatedEvent(Guid snapshotId, Guid chapterId, Guid authorId)
    {
        SnapshotId = snapshotId;
        ChapterId = chapterId;
        AuthorId = authorId;
    }
}

public sealed class DraftPublishedEvent : DomainEvent
{
    public Guid DraftId { get; }
    public Guid ChapterId { get; }
    public Guid PublishedSnapshotId { get; }

    public DraftPublishedEvent(Guid draftId, Guid chapterId, Guid publishedSnapshotId)
    {
        DraftId = draftId;
        ChapterId = chapterId;
        PublishedSnapshotId = publishedSnapshotId;
    }
}

public sealed class BookCreatedEvent : DomainEvent
{
    public Guid BookId { get; }
    public Guid AuthorId { get; }
    public string Title { get; }

    public BookCreatedEvent(Guid bookId, Guid authorId, string title)
    {
        BookId = bookId;
        AuthorId = authorId;
        Title = title;
    }
}

public sealed class ChapterCreatedEvent : DomainEvent
{
    public Guid ChapterId { get; }
    public Guid BookId { get; }
    public string Title { get; }

    public ChapterCreatedEvent(Guid chapterId, Guid bookId, string title)
    {
        ChapterId = chapterId;
        BookId = bookId;
        Title = title;
    }
}
