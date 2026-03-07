using MediatR;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Domain event dispatcher — MediatR INotification ile event handler'ları çağırır.
/// SaveChanges sonrası UnitOfWork tarafından tetiklenir.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
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

            // MediatR notification olarak yayınla (INotification implement eden handler'lar yakalar)
            await _mediator.Publish((object)domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
