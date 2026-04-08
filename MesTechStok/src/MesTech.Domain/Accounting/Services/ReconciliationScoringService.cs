using System.Globalization;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mutabakat eslestirme skorlama servisi.
/// 3 bileskenli skor: tutar (%60), tarih (%25), aciklama (%15).
/// Esik degerleri: >= 0.85 AutoMatched, 0.70-0.84 NeedsReview, &lt; 0.70 Unmatched.
/// </summary>
public sealed class ReconciliationScoringService : IReconciliationScoringService
{
    /// <inheritdoc />
    public decimal AutoMatchThreshold => 0.85m;

    /// <inheritdoc />
    public decimal ReviewThreshold => 0.70m;

    private static readonly Dictionary<string, int> PlatformPaymentWindows = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = 7,
        ["Amazon"] = 14,
        ["Hepsiburada"] = 10,
        ["N11"] = 10,
        ["Ciceksepeti"] = 14,
        ["Pazarama"] = 7
    };

    private static readonly Dictionary<string, string[]> KnownPayers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = ["DSM Grup", "DSM GRUP", "DSMGRUP", "DSM", "Trendyol"],
        ["Hepsiburada"] = ["D-MARKET", "D-Market", "DMARKET", "Hepsiburada"],
        ["N11"] = ["Doğuş Teknoloji", "N11", "N11.COM", "DOGUS"],
        ["Ciceksepeti"] = ["Çiçeksepeti", "Ciceksepeti", "CICEKSEPETI", "CS"],
        ["Amazon"] = ["Amazon", "AMAZON", "Amazon Payments", "AMZN"],
        ["Pazarama"] = ["Pazarama", "PAZARAMA", "PZR"]
    };

    private static readonly Dictionary<string, string[]> PlatformAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = ["TY", "TRENDYOL", "DSM", "Trendyol"],
        ["Hepsiburada"] = ["HB", "HEPSIBURADA", "HEPSI", "D-MARKET"],
        ["N11"] = ["N11", "N11.COM"],
        ["Ciceksepeti"] = ["CS", "CICEKSEPETI", "CICEK", "ÇİÇEKSEPETİ"],
        ["Amazon"] = ["AMAZON", "AMZ", "AMZN"],
        ["Pazarama"] = ["PAZARAMA", "PZR"]
    };

    /// <inheritdoc />
    public decimal CalculateConfidence(
        decimal bankAmount,
        decimal settlementAmount,
        DateTime bankDate,
        DateTime settlementDate,
        string? bankDescription = null,
        string? settlementPlatform = null)
    {
        // 3-component weighted formula: amount 60% + date 25% + description 15%
        var amountScore = CalculateAmountScore(settlementAmount, bankAmount);
        var dateScore = CalculateDirectDateScore(bankDate, settlementDate);
        var descriptionScore = CalculateLegacyDescriptionScore(bankDescription, settlementPlatform);

        var totalScore = (amountScore * 0.60m)
                       + (dateScore * 0.25m)
                       + (descriptionScore * 0.15m);

        return Math.Round(Math.Clamp(totalScore, 0m, 1m), 4);
    }

    private static decimal CalculateDirectDateScore(DateTime bankDate, DateTime settlementDate)
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

    private static decimal CalculateLegacyDescriptionScore(string? bankDescription, string? settlementPlatform)
    {
        if (string.IsNullOrWhiteSpace(bankDescription) || string.IsNullOrWhiteSpace(settlementPlatform))
            return 0.5m; // Belirsiz — nötr skor

        if (bankDescription.Contains(settlementPlatform, StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        if (PlatformAliases.TryGetValue(settlementPlatform, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (bankDescription.Contains(alias, StringComparison.OrdinalIgnoreCase))
                    return 0.9m;
            }
        }

        return 0.2m;
    }

    /// <inheritdoc />
    public decimal CalculateAmountScore(decimal expected, decimal actual)
    {
        if (expected == 0 && actual == 0) return 1m;
        if (expected == 0 || actual == 0) return 0m;

        var absExpected = Math.Abs(expected);
        var absActual = Math.Abs(actual);

        // Exact match
        if (absExpected == absActual) return 1m;

        var difference = Math.Abs(absExpected - absActual);
        var percentDiff = difference / absExpected;

        // Tolerance: +/- 0.5% => perfect score
        if (percentDiff <= 0.005m) return 1.0m;
        // Within 1%
        if (percentDiff <= 0.01m) return 0.95m;
        // Within 2%
        if (percentDiff <= 0.02m) return 0.85m;
        // Within 5%
        if (percentDiff <= 0.05m) return 0.70m;
        // Within 10%
        if (percentDiff <= 0.10m) return 0.40m;

        return 0m;
    }

    /// <inheritdoc />
    public decimal CalculateDateScore(DateTime periodEnd, DateTime txDate, string platform)
    {
        var paymentWindow = GetPlatformPaymentWindow(platform);
        var expectedPaymentDate = periodEnd.AddDays(paymentWindow);
        var daysDiff = Math.Abs((txDate.Date - expectedPaymentDate.Date).Days);

        return daysDiff switch
        {
            0 => 1.0m,
            1 => 0.95m,
            2 => 0.90m,
            3 => 0.80m,
            <= 5 => 0.60m,
            <= 7 => 0.40m,
            <= 10 => 0.20m,
            _ => 0m
        };
    }

    /// <inheritdoc />
    public decimal CalculateTextScore(string? payoutRef, string? platform, string? txDescription)
    {
        if (string.IsNullOrWhiteSpace(txDescription))
            return 0.3m; // Unknown — low-neutral score

        var score = 0m;

        // Check payoutRef in description
        if (!string.IsNullOrWhiteSpace(payoutRef)
            && txDescription.Contains(payoutRef, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.5m;
        }

        // Check platform name / aliases in description
        if (!string.IsNullOrWhiteSpace(platform))
        {
            if (txDescription.Contains(platform, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.5m;
            }
            else if (PlatformAliases.TryGetValue(platform, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (txDescription.Contains(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.4m;
                        break;
                    }
                }
            }
        }

        return Math.Clamp(score, 0m, 1m);
    }

    /// <inheritdoc />
    public decimal CalculateCounterpartyScore(string? platform, string? txCounterpartyName)
    {
        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(txCounterpartyName))
            return 0.3m; // Unknown — low-neutral score

        if (!KnownPayers.TryGetValue(platform, out var knownNames))
            return 0.3m;

        foreach (var name in knownNames)
        {
            if (txCounterpartyName.Contains(name, StringComparison.OrdinalIgnoreCase))
                return 1.0m;
        }

        return 0.1m;
    }

    /// <inheritdoc />
    public int GetPlatformPaymentWindow(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
            return 7; // Default: T+7

        return PlatformPaymentWindows.TryGetValue(platform, out var days) ? days : 7;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string[]> GetKnownPlatformPayers()
        => KnownPayers;
}
