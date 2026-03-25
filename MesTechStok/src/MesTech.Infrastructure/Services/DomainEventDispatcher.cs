using MediatR;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Domain event dispatcher — MediatR INotification ile event handler'ları çağırır.
/// SaveChanges sonrası UnitOfWork tarafından tetiklenir.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IMediator mediator, ILogger<DomainEventDispatcher> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var domainEvent in events)
        {
            _logger.LogInformation(
                "[DomainEvent] {EventType} at {OccurredAt}",
                domainEvent.GetType().Name,
                domainEvent.OccurredAt);

            // Domain event'i DomainEventNotification<T> ile sararak MediatR'a publish et.
            // IDomainEvent, INotification implement etmez (Domain sifir NuGet kurali),
            // wrapper sayesinde INotificationHandler<DomainEventNotification<T>> tetiklenir.
            var notificationType = typeof(DomainEventNotification<>)
                .MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)!;
            await _mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
