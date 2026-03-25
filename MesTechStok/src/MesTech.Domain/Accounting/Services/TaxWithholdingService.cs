namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Stopaj (tevkifat) hesaplama servisi.
/// 9284 CB kurali: matrah = KDV HARIC tutar; komisyon/kargo matrahi DEGISTIRMEZ.
/// </summary>
public sealed class TaxWithholdingService : ITaxWithholdingService
{
    /// <inheritdoc />
    public decimal CalculateWithholding(decimal taxExclusiveAmount, decimal rate)
    {
        if (rate < 0 || rate > 1)
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be between 0 and 1.");

        // 9284 CB: Stopaj matrahi KDV haric tutardir.
        // Komisyon ve kargo tutarlari matrahi degistirmez.
        return Math.Round(taxExclusiveAmount * rate, 2);
    }

    /// <inheritdoc />
    public decimal ExtractTaxExclusiveAmount(decimal taxInclusiveAmount, decimal kdvRate)
    {
        if (kdvRate < 0)
            throw new ArgumentOutOfRangeException(nameof(kdvRate), "KDV rate must be non-negative.");

        // KDV haric = KDV dahil / (1 + KDV orani)
        return Math.Round(taxInclusiveAmount / (1 + kdvRate), 2);
    }
}
