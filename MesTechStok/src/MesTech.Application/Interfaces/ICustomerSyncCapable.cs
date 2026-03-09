using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Musteri senkronizasyonu destekleyen platform adaptörleri icin interface.
/// Bidirectional: Pull (platform → local) ve Push (local → platform).
/// </summary>
public interface ICustomerSyncCapable
{
    Task<IReadOnlyList<CustomerSyncDto>> PullCustomersAsync(DateTime? since = null, CancellationToken ct = default);
    Task<bool> PushCustomerAsync(CustomerSyncDto customer, CancellationToken ct = default);
}
