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
