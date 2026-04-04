using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Amortisman hesaplama domain servisi — VUK md. 315-321.
/// FixedAsset entity'si uzerinden yillik amortisman ve tam tablo hesaplar.
///
/// Normal (Esit Payli): Maliyet / Faydali Omur = Yillik Amortisman.
/// Azalan Bakiyeler:   (Kalan Deger) * (1/Omur * 2).
///   - Oran normal yontemin 2 katini asamaz.
///   - Son yil: kalan bakiye tamamen amortize edilir.
///   - Azalan miktar normal yontemin altina dustugunde normal oran uygulanir.
/// </summary>
public sealed class DepreciationCalculationService
{
    /// <summary>
    /// Belirli bir sabit kiymet icin yillik amortisman tutarini hesaplar.
    /// Birikmis amortismani dikkate alarak CARI yilin tutarini dondurur.
    /// </summary>
    public decimal CalculateAnnual(FixedAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.NetBookValue <= 0)
            return 0m;

        if (asset.UsefulLifeYears <= 0)
            throw new ArgumentException("Faydali omur 0 veya negatif olamaz.", nameof(asset));

        return asset.Method switch
        {
            DepreciationMethod.StraightLine => CalculateStraightLineAnnual(asset),
            DepreciationMethod.DecliningBalance => CalculateDecliningBalanceAnnual(asset),
            _ => throw new ArgumentOutOfRangeException(nameof(asset), $"Desteklenmeyen amortisman yontemi: {asset.Method}")
        };
    }

    /// <summary>
    /// Sabit kiymetin tam amortisman tablosunu olusturur (tum yillar).
    /// </summary>
    public IReadOnlyList<DepreciationScheduleEntry> GenerateSchedule(FixedAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.UsefulLifeYears <= 0)
            throw new ArgumentException("Faydali omur 0 veya negatif olamaz.", nameof(asset));

        return asset.Method switch
        {
            DepreciationMethod.StraightLine => GenerateStraightLineSchedule(asset),
            DepreciationMethod.DecliningBalance => GenerateDecliningSchedule(asset),
            _ => throw new ArgumentOutOfRangeException(nameof(asset), $"Desteklenmeyen amortisman yontemi: {asset.Method}")
        };
    }

    private static decimal CalculateStraightLineAnnual(FixedAsset asset)
    {
        var yearlyAmount = Math.Round(asset.AcquisitionCost / asset.UsefulLifeYears, 2);
        return Math.Min(yearlyAmount, asset.NetBookValue);
    }

    private static decimal CalculateDecliningBalanceAnnual(FixedAsset asset)
    {
        var rate = 2m / asset.UsefulLifeYears;
        var declining = Math.Round(asset.NetBookValue * rate, 2);
        var linearMin = Math.Round(asset.AcquisitionCost / asset.UsefulLifeYears, 2);

        // Azalan miktar linear minimum altina dustugunde linear kullan
        var depreciation = Math.Max(declining, linearMin);
        return Math.Min(depreciation, asset.NetBookValue);
    }

    private static List<DepreciationScheduleEntry> GenerateStraightLineSchedule(FixedAsset asset)
    {
        var schedule = new List<DepreciationScheduleEntry>();
        var yearlyAmount = Math.Round(asset.AcquisitionCost / asset.UsefulLifeYears, 2);
        var accumulated = 0m;

        for (int year = 1; year <= asset.UsefulLifeYears; year++)
        {
            var depreciation = year == asset.UsefulLifeYears
                ? asset.AcquisitionCost - accumulated
                : yearlyAmount;

            accumulated += depreciation;

            schedule.Add(new DepreciationScheduleEntry(
                Year: year,
                DepreciationAmount: Math.Round(depreciation, 2),
                AccumulatedDepreciation: Math.Round(accumulated, 2),
                NetBookValue: Math.Round(asset.AcquisitionCost - accumulated, 2)));
        }

        return schedule;
    }

    private static List<DepreciationScheduleEntry> GenerateDecliningSchedule(FixedAsset asset)
    {
        var schedule = new List<DepreciationScheduleEntry>();
        var rate = 2m / asset.UsefulLifeYears;
        var bookValue = asset.AcquisitionCost;
        var accumulated = 0m;
        var linearAmount = Math.Round(asset.AcquisitionCost / asset.UsefulLifeYears, 2);

        for (int year = 1; year <= asset.UsefulLifeYears; year++)
        {
            decimal depreciation;

            if (year == asset.UsefulLifeYears)
            {
                // Son yil: kalan deger tamamen yazilir
                depreciation = bookValue;
            }
            else
            {
                depreciation = Math.Round(bookValue * rate, 2);
                // Minimum linear kontrol — VUK kurali
                if (depreciation < linearAmount)
                    depreciation = Math.Min(linearAmount, bookValue);
            }

            accumulated += depreciation;
            bookValue -= depreciation;

            schedule.Add(new DepreciationScheduleEntry(
                Year: year,
                DepreciationAmount: Math.Round(depreciation, 2),
                AccumulatedDepreciation: Math.Round(accumulated, 2),
                NetBookValue: Math.Round(bookValue, 2)));
        }

        return schedule;
    }
}
