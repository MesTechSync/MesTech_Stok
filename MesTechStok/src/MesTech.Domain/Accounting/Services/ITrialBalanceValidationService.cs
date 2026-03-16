namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mizan dogrulama servisi arayuzu.
/// Borc = Alacak kuralini belirli bir donem icin dogrular.
/// Muhasebe temel kurali: toplam borc her zaman toplam alacaga esit olmalidir.
/// </summary>
public interface ITrialBalanceValidationService
{
    /// <summary>
    /// Belirli bir donem icin mizan dogrulamasi yapar.
    /// Tum onaylanmis (posted) yevmiye kayitlarinin borc ve alacak toplamlarini karsilastirir.
    /// </summary>
    /// <param name="tenantId">Kiraci kimligi.</param>
    /// <param name="startDate">Donem baslangic tarihi.</param>
    /// <param name="endDate">Donem bitis tarihi.</param>
    /// <param name="ct">Iptal tokeni.</param>
    /// <returns>Mizan dogrulama sonucu.</returns>
    Task<TrialBalanceValidationResult> ValidateAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);
}

/// <summary>
/// Mizan dogrulama sonucu.
/// IsBalanced == true ise toplam borc ve alacak esittir.
/// Errors listesi dengesiz yevmiye kayitlarini icerir.
/// </summary>
public record TrialBalanceValidationResult(
    bool IsBalanced,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal Difference,
    int JournalEntryCount,
    IReadOnlyList<string> Errors);
