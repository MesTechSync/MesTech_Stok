using MesTech.Application.DTOs.Cargo;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Kargo firmasi entegrasyon kontrati.
/// Her kargo firmasi (Yurtici, Aras, Surat vb.) bunu implement eder.
/// IIntegratorAdapter'dan BAGIMSIZ — ayri concern.
/// </summary>
public interface ICargoAdapter
{
    CargoProvider Provider { get; }

    Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default);
    Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default);
    Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default);
    Task<byte[]> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    bool SupportsCancellation { get; }
    bool SupportsLabelGeneration { get; }
    bool SupportsCashOnDelivery { get; }
    bool SupportsMultiParcel { get; }
}
