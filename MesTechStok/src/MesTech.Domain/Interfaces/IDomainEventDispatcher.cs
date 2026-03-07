using MesTech.Domain.Common;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Domain Event'leri MediatR'a dağıtan dispatcher interface'i.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
