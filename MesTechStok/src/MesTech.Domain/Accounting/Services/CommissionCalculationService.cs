namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Platform komisyon hesaplama servisi.
/// Varsayilan oranlar: Trendyol %15, Hepsiburada %18, N11 %12, Ciceksepeti %20.
/// </summary>
public class CommissionCalculationService : ICommissionCalculationService
{
    private static readonly Dictionary<string, decimal> DefaultRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = 0.15m,
        ["Hepsiburada"] = 0.18m,
        ["N11"] = 0.12m,
        ["Ciceksepeti"] = 0.20m,
        ["Amazon"] = 0.15m,
        ["Pazarama"] = 0.10m
    };

    public decimal CalculateCommission(string platform, string? category, decimal grossAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        if (grossAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "Gross amount must be non-negative.");

        var rate = GetDefaultRate(platform);
        return Math.Round(grossAmount * rate, 2);
    }

    public decimal GetDefaultRate(string platform)
    {
        return DefaultRates.TryGetValue(platform, out var rate) ? rate : 0.15m;
    }
}
