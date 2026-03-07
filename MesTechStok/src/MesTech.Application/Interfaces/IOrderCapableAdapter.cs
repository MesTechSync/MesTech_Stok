using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Siparis yonetimi destekleyen platform adaptörleri icin interface.
/// </summary>
public interface IOrderCapableAdapter
{
    Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default);
    Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default);
}
