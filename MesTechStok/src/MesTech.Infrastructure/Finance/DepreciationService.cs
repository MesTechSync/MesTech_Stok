using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Infrastructure.Finance;

/// <summary>
/// Amortisman hesaplama servisi.
/// Dogrusal (linear): Maliyet / Faydali Omur = Yillik Amortisman.
/// Azalan Bakiyeler (declining): (Kalan Deger * 2) / Faydali Omur — cift azalan.
/// VUK 315. madde: Amortismana tabi iktisadi kiymetler.
/// </summary>
public sealed class DepreciationService : IDepreciationService
{
    /// <inheritdoc />
    public List<DepreciationScheduleDto> CalculateDepreciation(
        decimal cost,
        int usefulLifeYears,
        string method)
    {
        if (cost <= 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Maliyet sifirdan buyuk olmalidir.");
        if (usefulLifeYears <= 0)
            throw new ArgumentOutOfRangeException(nameof(usefulLifeYears), "Faydali omur sifirdan buyuk olmalidir.");
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return method.ToLowerInvariant() switch
        {
            "linear" or "dogrusal" => CalculateLinear(cost, usefulLifeYears),
            "declining" or "azalan" => CalculateDeclining(cost, usefulLifeYears),
            _ => throw new ArgumentException(
                $"Desteklenmeyen amortisman yontemi: '{method}'. 'linear' veya 'declining' kullanin.",
                nameof(method))
        };
    }

    /// <summary>
    /// Dogrusal (esit payli) amortisman.
    /// Yillik amortisman = Maliyet / Faydali Omur.
    /// </summary>
    private static List<DepreciationScheduleDto> CalculateLinear(decimal cost, int usefulLifeYears)
    {
        var schedule = new List<DepreciationScheduleDto>();
        var yearlyAmount = Math.Round(cost / usefulLifeYears, 2);
        var accumulated = 0m;

        for (int year = 1; year <= usefulLifeYears; year++)
        {
            // Last year: adjust for rounding
            var depreciation = year == usefulLifeYears
                ? cost - accumulated
                : yearlyAmount;

            accumulated += depreciation;
            var bookValue = cost - accumulated;

            schedule.Add(new DepreciationScheduleDto
            {
                Year = year,
                DepreciationAmount = Math.Round(depreciation, 2),
                AccumulatedDepreciation = Math.Round(accumulated, 2),
                BookValue = Math.Round(bookValue, 2)
            });
        }

        return schedule;
    }

    /// <summary>
    /// Azalan bakiyeler (cift azalan) amortisman.
    /// Oran = (1 / Faydali Omur) * 2.
    /// Son yil: kalan deger tamamen amortismana alinir.
    /// VUK cift azalan bakiyeler yontemi (md. 315).
    /// </summary>
    private static List<DepreciationScheduleDto> CalculateDeclining(decimal cost, int usefulLifeYears)
    {
        var schedule = new List<DepreciationScheduleDto>();
        var rate = 2m / usefulLifeYears;
        var bookValue = cost;
        var accumulated = 0m;

        for (int year = 1; year <= usefulLifeYears; year++)
        {
            decimal depreciation;

            if (year == usefulLifeYears)
            {
                // Son yil: kalan deger tamamen yazilir
                depreciation = bookValue;
            }
            else
            {
                depreciation = Math.Round(bookValue * rate, 2);
                // Minimum linear amount check
                var linearAmount = Math.Round(cost / usefulLifeYears, 2);
                if (depreciation < linearAmount)
                    depreciation = Math.Min(linearAmount, bookValue);
            }

            accumulated += depreciation;
            bookValue -= depreciation;

            schedule.Add(new DepreciationScheduleDto
            {
                Year = year,
                DepreciationAmount = Math.Round(depreciation, 2),
                AccumulatedDepreciation = Math.Round(accumulated, 2),
                BookValue = Math.Round(bookValue, 2)
            });
        }

        return schedule;
    }
}
