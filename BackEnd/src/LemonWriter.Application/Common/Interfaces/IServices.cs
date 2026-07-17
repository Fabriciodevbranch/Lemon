using LemonWriter.Domain.Common;

namespace LemonWriter.Application.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

public interface IExportService
{
    Task<byte[]> ExportBookAsync(Guid bookId, string format, CancellationToken cancellationToken = default);
}

public interface IResourceAuthorizationService
{
    Task<bool> OwnsBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
    Task<bool> OwnsChapterAsync(Guid userId, Guid chapterId, CancellationToken cancellationToken = default);
    Task<bool> OwnsDraftAsync(Guid userId, Guid draftId, CancellationToken cancellationToken = default);
    Task<bool> OwnsSnapshotAsync(Guid userId, Guid snapshotId, CancellationToken cancellationToken = default);
}
