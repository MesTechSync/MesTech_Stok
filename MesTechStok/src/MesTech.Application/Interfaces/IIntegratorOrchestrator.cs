using MesTech.Application.DTOs;
using MesTech.Domain.Events;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Çoklu platform orkestrasyon arayüzü.
/// </summary>
public interface IIntegratorOrchestrator
{
    IReadOnlyList<IIntegratorAdapter> RegisteredAdapters { get; }

    Task RegisterAdapterAsync(IIntegratorAdapter adapter);
    Task RemoveAdapterAsync(string platformCode);

    Task<SyncResultDto> SyncAllPlatformsAsync(CancellationToken ct = default);
    Task<SyncResultDto> SyncPlatformAsync(string platformCode, CancellationToken ct = default);

    // Event-driven: Stok değiştiğinde otomatik tüm platformlara push
    Task HandleStockChangedAsync(StockChangedEvent domainEvent, CancellationToken ct = default);
    Task HandlePriceChangedAsync(PriceChangedEvent domainEvent, CancellationToken ct = default);
    Task HandleProductCreatedAsync(ProductCreatedEvent domainEvent, CancellationToken ct = default);
}
