namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mutabakat eslestirme skorlama servisi.
/// Tutar, tarih ve aciklama benzerligine gore 0-1 arasi guven skoru hesaplar.
/// </summary>
public class ReconciliationScoringService : IReconciliationScoringService
{
    /// <inheritdoc />
    public decimal AutoMatchThreshold => 0.85m;

    /// <inheritdoc />
    public decimal CalculateConfidence(
        decimal bankAmount,
        decimal settlementAmount,
        DateTime bankDate,
        DateTime settlementDate,
        string? bankDescription = null,
        string? settlementPlatform = null)
    {
        // Tutar benzerlik skoru (%60 agirlik)
        var amountScore = CalculateAmountScore(bankAmount, settlementAmount);

        // Tarih benzerlik skoru (%25 agirlik)
        var dateScore = CalculateDateScore(bankDate, settlementDate);

        // Aciklama benzerlik skoru (%15 agirlik)
        var descriptionScore = CalculateDescriptionScore(bankDescription, settlementPlatform);

        var totalScore = (amountScore * 0.60m) + (dateScore * 0.25m) + (descriptionScore * 0.15m);

        return Math.Round(Math.Clamp(totalScore, 0m, 1m), 4);
    }

    private static decimal CalculateAmountScore(decimal bankAmount, decimal settlementAmount)
    {
        if (bankAmount == 0 && settlementAmount == 0) return 1m;
        if (bankAmount == 0 || settlementAmount == 0) return 0m;

        var absBankAmount = Math.Abs(bankAmount);
        var absSettlementAmount = Math.Abs(settlementAmount);

        // Tam esit
        if (absBankAmount == absSettlementAmount) return 1m;

        // Yuzde fark
        var difference = Math.Abs(absBankAmount - absSettlementAmount);
        var maxAmount = Math.Max(absBankAmount, absSettlementAmount);
        var percentDiff = difference / maxAmount;

        // %1'den az fark: yuksek skor
        if (percentDiff <= 0.01m) return 0.95m;
        // %5'ten az fark: iyi skor
        if (percentDiff <= 0.05m) return 0.80m;
        // %10'dan az fark: orta skor
        if (percentDiff <= 0.10m) return 0.50m;

        return 0m;
    }

    private static decimal CalculateDateScore(DateTime bankDate, DateTime settlementDate)
    {
        var daysDiff = Math.Abs((bankDate.Date - settlementDate.Date).Days);

        return daysDiff switch
        {
            0 => 1.0m,
            1 => 0.9m,
            2 => 0.8m,
            3 => 0.7m,
            <= 5 => 0.5m,
            <= 7 => 0.3m,
            _ => 0m
        };
    }

    private static decimal CalculateDescriptionScore(string? bankDescription, string? settlementPlatform)
    {
        if (string.IsNullOrWhiteSpace(bankDescription) || string.IsNullOrWhiteSpace(settlementPlatform))
            return 0.5m; // Belirsiz — nötr skor

        // Banka aciklamasinda platform adinin gecip gecmedigini kontrol et
        if (bankDescription.Contains(settlementPlatform, StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        // Bilinen platform kisaltmalari
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Trendyol"] = ["TY", "TRENDYOL", "DSM"],
            ["Hepsiburada"] = ["HB", "HEPSIBURADA", "HEPSI"],
            ["N11"] = ["N11", "N11.COM"],
            ["Ciceksepeti"] = ["CS", "CICEKSEPETI", "CICEK"],
            ["Amazon"] = ["AMAZON", "AMZ", "AMZN"],
            ["Pazarama"] = ["PAZARAMA", "PZR"]
        };

        if (aliases.TryGetValue(settlementPlatform, out var platformAliases))
        {
            foreach (var alias in platformAliases)
            {
                if (bankDescription.Contains(alias, StringComparison.OrdinalIgnoreCase))
                    return 0.9m;
            }
        }

        return 0.2m;
    }
}
