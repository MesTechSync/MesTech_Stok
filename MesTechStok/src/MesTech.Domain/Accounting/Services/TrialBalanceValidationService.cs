using MesTech.Domain.Accounting.Entities;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mizan dogrulama servisi.
/// Tum onaylanmis yevmiye kayitlari icin sum(debit) == sum(credit) kuralini dogrular.
/// Her bir yevmiye kaydi ayrica bireysel olarak kontrol edilir.
///
/// Dogrulama adimlari:
/// 1. Donem icindeki tum onaylanmis (IsPosted) JournalEntry'leri al
/// 2. Her entry'nin Lines'ini kontrol et: entry-bazli borc == alacak
/// 3. Genel toplam: tum satirlarin borc ve alacak toplami karsilastirilir
/// </summary>
public class TrialBalanceValidationService : ITrialBalanceValidationService
{
    /// <summary>
    /// Kurus altindaki farklar icin tolerans — floating point islemlerinden kaynaklanabilir.
    /// 0.01 kurus = 0.0001 TL tolerans.
    /// </summary>
    private const decimal Tolerance = 0.0001m;

    /// <inheritdoc />
    public Task<TrialBalanceValidationResult> ValidateAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        // Not: Bu servis domain servisi olarak tasarlanmistir.
        // Veri erisimi Application katmaninda handler tarafindan saglanir.
        // Bu metodun kullanimi icin ValidateFromEntries kullanilmalidir.
        throw new InvalidOperationException(
            "Use ValidateFromEntries for direct validation. " +
            "For async repository access, use the Application layer handler.");
    }

    /// <summary>
    /// JournalEntry listesi uzerinden mizan dogrulamasi yapar.
    /// Application katmanindaki handler repository'den veriyi alir ve bu metoda gonderir.
    /// </summary>
    /// <param name="entries">Donem icindeki onaylanmis yevmiye kayitlari.</param>
    /// <returns>Mizan dogrulama sonucu.</returns>
    public TrialBalanceValidationResult ValidateFromEntries(IReadOnlyList<JournalEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var errors = new List<string>();
        var totalDebits = 0m;
        var totalCredits = 0m;
        var postedEntries = entries.Where(e => e.IsPosted).ToList();

        foreach (var entry in postedEntries)
        {
            var entryDebit = entry.Lines.Sum(l => l.Debit);
            var entryCredit = entry.Lines.Sum(l => l.Credit);

            totalDebits += entryDebit;
            totalCredits += entryCredit;

            var entryDiff = Math.Abs(entryDebit - entryCredit);
            if (entryDiff > Tolerance)
            {
                errors.Add(
                    $"JournalEntry {entry.Id} ({entry.EntryDate:yyyy-MM-dd}, '{entry.Description}'): " +
                    $"Borc={entryDebit:N2} TL, Alacak={entryCredit:N2} TL, Fark={entryDiff:N2} TL");
            }
        }

        var difference = totalDebits - totalCredits;
        var isBalanced = Math.Abs(difference) <= Tolerance;

        if (!isBalanced && errors.Count == 0)
        {
            errors.Add($"Genel mizan dengesizligi: Fark={difference:N2} TL");
        }

        return new TrialBalanceValidationResult(
            IsBalanced: isBalanced,
            TotalDebits: Math.Round(totalDebits, 2),
            TotalCredits: Math.Round(totalCredits, 2),
            Difference: Math.Round(difference, 2),
            JournalEntryCount: postedEntries.Count,
            Errors: errors.AsReadOnly());
    }
}
