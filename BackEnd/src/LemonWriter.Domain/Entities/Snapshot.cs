using LemonWriter.Domain.Common;
using LemonWriter.Domain.Events;

namespace LemonWriter.Domain.Entities;

public class Snapshot : AggregateRoot<Guid>
{
    public Guid ChapterId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string SnapshotMessage { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentSnapshotId { get; private set; }

    private Snapshot() { }

    public static Snapshot Create(
        Guid chapterId,
        string content,
        string snapshotMessage,
        Guid authorId,
        Guid? parentSnapshotId = null)
    {
        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            Content = content,
            SnapshotMessage = snapshotMessage,
            AuthorId = authorId,
            ParentSnapshotId = parentSnapshotId,
            CreatedAt = DateTime.UtcNow
        };
        snapshot.RaiseDomainEvent(new SnapshotCreatedEvent(snapshot.Id, snapshot.ChapterId, snapshot.AuthorId));
        return snapshot;
    }
}
