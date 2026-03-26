namespace MesTech.Domain.Interfaces;

/// <summary>
/// İşlenmiş event kaydı — idempotency guard repository.
/// </summary>
public interface IProcessedDomainEventRepository
{
    /// <summary>Bu EventId daha önce işlendi mi?</summary>
    Task<bool> IsProcessedAsync(Guid eventId, string handlerName, CancellationToken ct = default);

    /// <summary>Event'i işlenmiş olarak kaydet.</summary>
    Task MarkAsProcessedAsync(Guid eventId, string eventType, Guid tenantId, string handlerName, CancellationToken ct = default);
}
