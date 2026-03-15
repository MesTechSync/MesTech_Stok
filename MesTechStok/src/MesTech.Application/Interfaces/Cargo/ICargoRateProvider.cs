using MesTech.Application.DTOs.Cargo;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Cargo;

/// <summary>
/// Kargo fiyat sorgulama saglayicisi — gonderi bilgisine gore ucret ve teslimat suresi dondurur.
/// DEV 3 tarafindan implement edilecektir.
/// </summary>
public interface ICargoRateProvider
{
    Task<CargoRateResult?> GetRateAsync(
        ShipmentRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Kargo fiyat sorgulama sonucu.
/// </summary>
public record CargoRateResult(
    CargoProvider Provider,
    decimal Price,
    string Currency,             // "TRY"
    TimeSpan EstimatedDelivery,
    bool IncludesVat
);
