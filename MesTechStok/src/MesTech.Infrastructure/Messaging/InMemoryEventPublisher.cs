using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// In-memory IEventPublisher implementation for Desktop use.
/// Logs events but does not send them to an external bus.
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher>? _logger;

    public InMemoryEventPublisher() { }

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent
    {
        _logger?.LogDebug("InMemoryEventPublisher: {EventType} published (no-op)", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        if (domainEvents == null) return Task.CompletedTask;

        foreach (var evt in domainEvents)
        {
            _logger?.LogDebug("InMemoryEventPublisher: {EventType} published (no-op)", evt.GetType().Name);
        }

        return Task.CompletedTask;
    }
}
