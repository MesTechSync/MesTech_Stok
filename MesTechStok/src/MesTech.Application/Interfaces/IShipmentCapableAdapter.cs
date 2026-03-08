using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform'a kargo bildirimi yapabilen adapter capability.
/// Ornek: Trendyol'a "kargoya verdim" demek, Ciceksepeti'ne takip no gondermek.
/// ICargoAdapter (fiziksel kargo) ile karistirilmamali.
/// </summary>
public interface IShipmentCapableAdapter
{
    Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default);
}
