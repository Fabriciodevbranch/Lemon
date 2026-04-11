using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LemonWriter.Infrastructure.EventBus;

public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger) => _logger = logger;

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing domain event {EventType} with Id {EventId}", domainEvent.GetType().Name, domainEvent.Id);
        return Task.CompletedTask;
    }
}
