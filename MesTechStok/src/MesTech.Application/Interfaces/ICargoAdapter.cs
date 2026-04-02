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
    Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default);

    /// <summary>
    /// Gets shipment label in the preferred format. Falls back to PDF if not supported.
    /// </summary>
    Task<LabelResult> GetShipmentLabelAsync(string shipmentId, LabelFormat preferredFormat, CancellationToken ct = default)
        => GetShipmentLabelAsync(shipmentId, ct);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    /// <summary>AdapterHealthService ping — IsAvailableAsync wrapper.</summary>
    Task<bool> PingAsync(CancellationToken ct = default) => IsAvailableAsync(ct);

    bool SupportsCancellation { get; }
    bool SupportsLabelGeneration { get; }
    bool SupportsCashOnDelivery { get; }
    bool SupportsMultiParcel { get; }
}
