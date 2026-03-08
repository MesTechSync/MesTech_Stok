using MesTech.Domain.Enums;
using MesTech.Domain.Models;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Kargo firma adaptoru — her kargo firması (Yurtici, Aras, Surat vb.) bunu implement eder.
/// IIntegratorAdapter'dan bagimsiz kontrat.
/// </summary>
public interface ICargoAdapter
{
    CargoProvider Provider { get; }

    /// <summary>Gonderi olusturma.</summary>
    Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default);

    /// <summary>Takip sorgulama.</summary>
    Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default);

    /// <summary>Gonderi iptal.</summary>
    Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default);

    /// <summary>Etiket yazdirma (PDF byte array).</summary>
    Task<byte[]> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default);

    /// <summary>Kargo firmasi musaitlik kontrolu.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    // Capability flags
    bool SupportsCancellation { get; }
    bool SupportsLabelGeneration { get; }
    bool SupportsCashOnDelivery { get; }
    bool SupportsMultiParcel { get; }
}
