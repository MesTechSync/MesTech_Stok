using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Otomatik kargo atama domain servisi.
/// Siparis bilgilerine gore en uygun kargo firmasini belirler.
/// Saf is kurallari, altyapi bagimliligi yok.
/// </summary>
public class AutoShipmentService : IAutoShipmentService
{
    /// <summary>
    /// Buyuk sehirler listesi — ozel kargo sozlesmesi olan iller.
    /// </summary>
    private static readonly HashSet<string> MetropolitanCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Istanbul", "Ankara", "Izmir", "Bursa", "Antalya",
        "Adana", "Konya", "Gaziantep", "Mersin", "Kayseri"
    };

    /// <summary>
    /// Agir paket esik degeri (kg).
    /// </summary>
    private const decimal HeavyWeightThresholdKg = 30m;

    /// <summary>
    /// Buyuk desi esik degeri.
    /// </summary>
    private const decimal LargeDesiThreshold = 50m;

    /// <summary>
    /// Platform bazinda tercih edilen kargo saglayicilari.
    /// </summary>
    private static readonly Dictionary<PlatformType, CargoProvider> PlatformPreferences = new()
    {
        [PlatformType.Trendyol] = CargoProvider.YurticiKargo,
        [PlatformType.Hepsiburada] = CargoProvider.Hepsijet,
        [PlatformType.N11] = CargoProvider.ArasKargo,
        [PlatformType.Ciceksepeti] = CargoProvider.MngKargo,
        [PlatformType.Amazon] = CargoProvider.UPS
    };

    /// <summary>
    /// Siparis bilgilerine gore en uygun kargo saglayiciyi oner.
    /// Oncelik sirasi:
    /// 1. Platform tercihi (varsa)
    /// 2. Kapida odeme destegi
    /// 3. Agirlik/desi sinifi
    /// 4. Hedef sehir (buyuksehir vs tasra)
    /// </summary>
    public ShipmentRecommendation Recommend(ShipmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DestinationCity);

        // Kural 1: Platform tercihine bak
        if (request.SourcePlatform.HasValue &&
            PlatformPreferences.TryGetValue(request.SourcePlatform.Value, out var platformProvider))
        {
            // Hepsijet sadece buyuksehirlerde gecerli
            if (platformProvider == CargoProvider.Hepsijet &&
                !MetropolitanCities.Contains(request.DestinationCity))
            {
                return new ShipmentRecommendation(
                    CargoProvider.YurticiKargo,
                    $"Hepsiburada siparisi ancak {request.DestinationCity} buyuksehir degil, Yurtici Kargo atandi.");
            }

            return new ShipmentRecommendation(
                platformProvider,
                $"{request.SourcePlatform.Value} platformu icin tercih edilen kargo: {platformProvider}.");
        }

        // Kural 2: Kapida odeme — sadece belirli firmalar destekler
        if (request.IsCashOnDelivery)
        {
            return new ShipmentRecommendation(
                CargoProvider.YurticiKargo,
                "Kapida odeme siparisi — Yurtici Kargo (COD destekli) atandi.");
        }

        // Kural 3: Agir/buyuk paketler
        if (request.WeightKg >= HeavyWeightThresholdKg || request.Desi >= LargeDesiThreshold)
        {
            return new ShipmentRecommendation(
                CargoProvider.ArasKargo,
                $"Agir/buyuk paket (Agirlik: {request.WeightKg}kg, Desi: {request.Desi}) — Aras Kargo atandi.");
        }

        // Kural 4: Buyuksehir → Surat Kargo (hizli teslimat), tasra → PTT Kargo (genis ag)
        if (MetropolitanCities.Contains(request.DestinationCity))
        {
            return new ShipmentRecommendation(
                CargoProvider.SuratKargo,
                $"{request.DestinationCity} buyuksehir — Surat Kargo (hizli teslimat) atandi.");
        }

        return new ShipmentRecommendation(
            CargoProvider.PttKargo,
            $"{request.DestinationCity} tasra bolge — PTT Kargo (genis dag ag) atandi.");
    }
}
