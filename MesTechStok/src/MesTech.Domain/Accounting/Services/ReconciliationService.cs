using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mutabakat eslestirme servisi.
/// SettlementLine'lar ile BankTransaction'lari karstilastirir,
/// en iyi eslestirmeyi bulan greedy algoritma kullanir.
///
/// Guven skoru (Confidence) 0-1 arasi:
///   0.95-1.00: OrderId tam eslestirme + tutar tam eslestirme
///   0.80-0.94: OrderId tam eslestirme + tutar %1 tolerans icinde
///   0.60-0.79: Tutar eslestirme + tarih 3 gun icinde (OrderId yok)
///   0.60 alti: Manuel inceleme gerekli — eslestirme olusturulmaz
/// </summary>
public class ReconciliationService : IReconciliationService
{
    /// <summary>
    /// Tutar toleransi: %1.
    /// </summary>
    private const decimal AmountTolerancePercent = 0.01m;

    /// <summary>
    /// Minimum guven skoru esik degeri — altindaki eslestirmeler olusturulmaz.
    /// </summary>
    private const decimal MinimumConfidenceThreshold = 0.60m;

    /// <summary>
    /// Otomatik eslestirme esik degeri — bu skor ve uzerinde AutoMatched durumu atanir.
    /// </summary>
    private const decimal AutoMatchThreshold = 0.80m;

    /// <inheritdoc />
    public IReadOnlyList<ReconciliationMatch> Reconcile(
        Guid tenantId,
        IReadOnlyList<SettlementLine> lines,
        IReadOnlyList<BankTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(transactions);

        if (lines.Count == 0 || transactions.Count == 0)
            return Array.Empty<ReconciliationMatch>();

        // Build candidate scores for all line x tx pairs
        var candidates = new List<(SettlementLine Line, BankTransaction Tx, decimal Score)>();

        foreach (var line in lines)
        {
            foreach (var tx in transactions)
            {
                var score = CalculateMatchScore(line, tx);
                if (score >= MinimumConfidenceThreshold)
                {
                    candidates.Add((line, tx, score));
                }
            }
        }

        // Sort by score descending — greedy: best match wins
        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

        var matchedLineIds = new HashSet<Guid>();
        var matchedTxIds = new HashSet<Guid>();
        var results = new List<ReconciliationMatch>();

        foreach (var (line, tx, score) in candidates)
        {
            // Each line and transaction can only be matched once
            if (matchedLineIds.Contains(line.Id) || matchedTxIds.Contains(tx.Id))
                continue;

            var status = score >= AutoMatchThreshold
                ? ReconciliationStatus.AutoMatched
                : ReconciliationStatus.NeedsReview;

            var match = ReconciliationMatch.Create(
                tenantId,
                DateTime.UtcNow,
                score,
                status,
                line.SettlementBatchId,
                tx.Id);

            results.Add(match);
            matchedLineIds.Add(line.Id);
            matchedTxIds.Add(tx.Id);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Settlement line ile bank transaction arasindaki eslestirme skorunu hesaplar.
    /// 3 boyutlu skor: OrderId eslestirme + Tutar eslestirme + Tarih yakinligi.
    /// </summary>
    internal static decimal CalculateMatchScore(SettlementLine line, BankTransaction tx)
    {
        var hasOrderIdMatch = HasOrderIdMatch(line, tx);
        var amountMatch = CalculateAmountMatchScore(line.NetAmount, tx.Amount);
        var dateMatch = CalculateDateMatchScore(line.CreatedAt, tx.TransactionDate);

        // Scenario 1: OrderId exact match + exact amount → 0.95-1.00
        if (hasOrderIdMatch && amountMatch >= 1.0m)
            return 1.0m;

        // Scenario 2: OrderId exact match + amount within 1% → 0.80-0.94
        if (hasOrderIdMatch && amountMatch >= 0.80m)
            return 0.80m + (amountMatch * 0.14m);

        // Scenario 3: OrderId match but amount off → still decent
        if (hasOrderIdMatch)
            return 0.70m + (amountMatch * 0.10m);

        // Scenario 4: No OrderId match — amount match + date within tolerance → 0.60-0.79
        if (amountMatch >= 0.80m && dateMatch >= 0.80m)
            return 0.60m + (amountMatch * 0.10m) + (dateMatch * 0.09m);

        // Below threshold — no match created
        return amountMatch * 0.40m + dateMatch * 0.15m;
    }

    /// <summary>
    /// OrderId eslestirme kontrolu.
    /// Settlement line'in OrderId'si banka hareketi aciklamasinda veya referans numarasinda aranir.
    /// </summary>
    private static bool HasOrderIdMatch(SettlementLine line, BankTransaction tx)
    {
        if (string.IsNullOrWhiteSpace(line.OrderId))
            return false;

        var orderId = line.OrderId.Trim();

        // Check in bank transaction description
        if (!string.IsNullOrWhiteSpace(tx.Description)
            && tx.Description.Contains(orderId, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check in reference number
        if (!string.IsNullOrWhiteSpace(tx.ReferenceNumber)
            && tx.ReferenceNumber.Contains(orderId, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Tutar eslestirme skoru: tam eslesme 1.0, %1 tolerans icinde 0.90-0.99, yoksa azalan skor.
    /// </summary>
    private static decimal CalculateAmountMatchScore(decimal expected, decimal actual)
    {
        if (expected == 0 && actual == 0) return 1.0m;
        if (expected == 0 || actual == 0) return 0m;

        var absExpected = Math.Abs(expected);
        var absActual = Math.Abs(actual);

        if (absExpected == absActual) return 1.0m;

        var percentDiff = Math.Abs(absExpected - absActual) / absExpected;

        if (percentDiff <= AmountTolerancePercent)
            return 0.90m + ((1.0m - percentDiff / AmountTolerancePercent) * 0.10m);

        if (percentDiff <= 0.05m)
            return 0.60m;

        if (percentDiff <= 0.10m)
            return 0.30m;

        return 0m;
    }

    /// <summary>
    /// Tarih yakinlik skoru: ayni gun 1.0, 1 gun 0.90, 2 gun 0.85, 3 gun 0.80, sonra duser.
    /// </summary>
    private static decimal CalculateDateMatchScore(DateTime lineDate, DateTime txDate)
    {
        var daysDiff = Math.Abs((lineDate.Date - txDate.Date).Days);

        return daysDiff switch
        {
            0 => 1.0m,
            1 => 0.90m,
            2 => 0.85m,
            3 => 0.80m,
            <= 5 => 0.50m,
            <= 7 => 0.30m,
            _ => 0m
        };
    }
}
