using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Infrastructure.Finance;

/// <summary>
/// Gelir vergisi hesaplama servisi.
/// Turkiye 2026 gelir vergisi dilimleri (GVK md. 103).
/// Dilimler:
///   0    - 110.000 → %15
///   110K - 230.000 → %20
///   230K - 580.000 → %27
///   580K - 3.000.000 → %35
///   3M+             → %40
/// </summary>
public sealed class IncomeTaxService : IIncomeTaxService
{
    /// <summary>
    /// 2026 Turkiye gelir vergisi dilimleri.
    /// (alt sinir, ust sinir, oran)
    /// </summary>
    private static readonly (decimal Lower, decimal Upper, decimal Rate)[] Brackets2026 =
    {
        (0m,         110_000m,     0.15m),
        (110_000m,   230_000m,     0.20m),
        (230_000m,   580_000m,     0.27m),
        (580_000m,   3_000_000m,   0.35m),
        (3_000_000m, decimal.MaxValue, 0.40m)
    };

    /// <inheritdoc />
    public IncomeTaxResultDto CalculateIncomeTax(decimal annualIncome, int year)
    {
        if (annualIncome < 0)
            throw new ArgumentOutOfRangeException(nameof(annualIncome), "Yillik gelir negatif olamaz.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Yil 2000-2100 araliginda olmalidir.");

        var brackets = GetBracketsForYear(year);
        var remaining = annualIncome;
        var totalTax = 0m;
        var details = new List<TaxBracketDetailDto>();

        foreach (var (lower, upper, rate) in brackets)
        {
            if (remaining <= 0)
                break;

            var bracketWidth = upper == decimal.MaxValue
                ? remaining
                : upper - lower;

            var taxableInBracket = Math.Min(remaining, bracketWidth);
            var taxInBracket = Math.Round(taxableInBracket * rate, 2);

            details.Add(new TaxBracketDetailDto
            {
                LowerBound = lower,
                UpperBound = upper == decimal.MaxValue ? annualIncome : upper,
                Rate = rate,
                TaxableAmountInBracket = taxableInBracket,
                TaxInBracket = taxInBracket
            });

            totalTax += taxInBracket;
            remaining -= taxableInBracket;
        }

        var effectiveRate = annualIncome > 0
            ? Math.Round(totalTax / annualIncome, 4)
            : 0m;

        return new IncomeTaxResultDto
        {
            TaxableIncome = annualIncome,
            TotalTax = totalTax,
            EffectiveRate = effectiveRate,
            Year = year,
            BracketDetails = details
        };
    }

    /// <summary>
    /// Vergi yilina gore dilimleri doner.
    /// Simdilik 2026 dilimleri tum yillar icin kullanilir.
    /// Yeni dilimler eklendiginde buraya switch-case eklenebilir.
    /// </summary>
    private static (decimal Lower, decimal Upper, decimal Rate)[] GetBracketsForYear(int year)
    {
        // 2026 brackets — extend here for future years
        return Brackets2026;
    }
}
