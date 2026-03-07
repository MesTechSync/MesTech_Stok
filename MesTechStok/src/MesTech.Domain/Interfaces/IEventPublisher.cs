using MesTech.Domain.Common;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Event yayıncı — RabbitMQ/MassTransit implementasyonu ile kullanılacak.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent;
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}
