using MesTech.Application.DTOs.Cargo;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetCargoComparison;

/// <summary>
/// Kargo saglayici fiyat karsilastirma sorgusu.
/// Verilen gonderi bilgisine gore tum saglayicilarin fiyat ve teslimat surelerini sorgular.
/// </summary>
public record GetCargoComparisonQuery(ShipmentRequest ShipmentRequest)
    : IRequest<CargoComparisonResult>;

/// <summary>
/// Kargo karsilastirma sonucu — tum saglayicilarin fiyat teklifleri.
/// </summary>
public record CargoComparisonResult
{
    /// <summary>
    /// Tum saglayicilarin fiyat teklifleri.
    /// </summary>
    public IReadOnlyList<CargoComparisonItem> Items { get; init; } = Array.Empty<CargoComparisonItem>();

    /// <summary>
    /// En ucuz saglayici.
    /// </summary>
    public CargoProvider? CheapestProvider { get; init; }

    /// <summary>
    /// En hizli saglayici.
    /// </summary>
    public CargoProvider? FastestProvider { get; init; }
}

/// <summary>
/// Tek bir saglayicinin fiyat teklifi.
/// </summary>
public record CargoComparisonItem
{
    public CargoProvider Provider { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "TRY";
    public TimeSpan EstimatedDelivery { get; init; }
    public bool IncludesVat { get; init; }
    public bool IsAvailable { get; init; }
    public string? ErrorMessage { get; init; }
}
