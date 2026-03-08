using MesTech.Application.DTOs.Cargo;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Otomatik gonderim orkestrasyon servisi.
/// Siparis → kargo firma sec → gonderi olustur → platform bildir → MESA event.
/// </summary>
public interface IAutoShipmentService
{
    Task<ShipmentResult> ProcessOrderAsync(Guid orderId, CancellationToken ct = default);
}
