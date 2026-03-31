using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Headless test scope — 7 kargo firması için tek generic adapter.
/// Gerçek API çağrısı yapılmaz. Sabit tracking no ve status döner.
/// </summary>
public sealed class InMemoryCargoAdapter : ICargoAdapter
{
    private static int _trackingCounter;

    public CargoProvider Provider { get; }
    public bool SupportsCancellation => true;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => Provider != CargoProvider.SuratKargo;
    public bool SupportsMultiParcel => false;

    public InMemoryCargoAdapter(CargoProvider provider)
    {
        Provider = provider;
    }

    public Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default)
    {
        var counter = Interlocked.Increment(ref _trackingCounter);
        return Task.FromResult(new ShipmentResult
        {
            Success = true,
            TrackingNumber = $"TEST-TRACK-{counter:D3}",
            ShipmentId = Guid.NewGuid().ToString()
        });
    }

    public Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
        => Task.FromResult(new TrackingResult
        {
            TrackingNumber = trackingNumber,
            Status = CargoStatus.InTransit,
            Events = new List<TrackingEvent>
            {
                new() { Status = CargoStatus.InTransit }
            }
        });

    public Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
        => Task.FromResult(new LabelResult
        {
            Data = Array.Empty<byte>(),
            Format = LabelFormat.Pdf,
            FileName = $"label-{shipmentId}.pdf"
        });

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true);
}
