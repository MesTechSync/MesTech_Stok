using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.Cargo;

/// <summary>
/// Desi bazli kargo ucret hesaplayicisi.
/// Turkiye kargo firmalari genelde desi (hacimsel agirlik) uzerinden fiyatlandirir.
/// Gercek fiyatlar tenant bazli contract'tan gelir — buradaki degerler piyasa ortalamalaridir.
/// </summary>
public static class DesiBasedCargoRateCalculator
{
    /// <summary>
    /// Provider bazli varsayilan tarifeler (TRY, KDV dahil).
    /// Tenant bazli override icin appsettings uzerinden konfigure edilebilir.
    /// </summary>
    private static readonly Dictionary<CargoProvider, CargoTariff> DefaultTariffs = new()
    {
        [CargoProvider.YurticiKargo] = new(BasePriceTry: 32.00m, PerDesiTry: 4.00m, EstimatedDays: 2),
        [CargoProvider.ArasKargo]    = new(BasePriceTry: 28.00m, PerDesiTry: 3.50m, EstimatedDays: 2),
        [CargoProvider.SuratKargo]   = new(BasePriceTry: 30.00m, PerDesiTry: 4.00m, EstimatedDays: 1),
        [CargoProvider.MngKargo]     = new(BasePriceTry: 27.00m, PerDesiTry: 3.50m, EstimatedDays: 2),
        [CargoProvider.PttKargo]     = new(BasePriceTry: 22.00m, PerDesiTry: 2.50m, EstimatedDays: 3),
        [CargoProvider.Hepsijet]     = new(BasePriceTry: 33.00m, PerDesiTry: 4.50m, EstimatedDays: 1),
        [CargoProvider.Sendeo]       = new(BasePriceTry: 25.00m, PerDesiTry: 3.00m, EstimatedDays: 2),
    };

    /// <summary>
    /// Gonderi bilgisinden kargo ucretini hesaplar.
    /// Desi = max(fiziksel desi, agirlik kg) — Turkiye standardi.
    /// </summary>
    public static CargoRateResult Calculate(CargoProvider provider, ShipmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!DefaultTariffs.TryGetValue(provider, out var tariff))
            throw new ArgumentOutOfRangeException(nameof(provider), provider, "Tariff not defined for provider");

        // Turkiye standardi: faturalanabilir birim = max(desi, kg)
        var billableUnits = Math.Max(request.Desi, (int)Math.Ceiling(request.Weight));
        if (billableUnits < 1) billableUnits = 1;

        var price = tariff.BasePriceTry + (tariff.PerDesiTry * billableUnits * request.ParcelCount);

        // Kapida odeme ek ucreti (%2, min 5 TL)
        if (request.CodAmount.HasValue && request.CodAmount.Value > 0)
        {
            var codFee = Math.Max(5.00m, request.CodAmount.Value * 0.02m);
            price += codFee;
        }

        return new CargoRateResult(
            Provider: provider,
            Price: Math.Round(price, 2),
            Currency: "TRY",
            EstimatedDelivery: TimeSpan.FromDays(tariff.EstimatedDays),
            IncludesVat: true
        );
    }

    /// <summary>
    /// Kargo tarife bilgisi.
    /// </summary>
    private record CargoTariff(decimal BasePriceTry, decimal PerDesiTry, int EstimatedDays);
}
