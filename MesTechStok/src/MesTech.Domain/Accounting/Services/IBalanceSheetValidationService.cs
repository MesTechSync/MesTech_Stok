namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Bilanco dogrulama servisi arayuzu.
/// Muhasebe temel denklemi: Varliklar == Borclar + Ozkaynaklar.
/// Revenue ve Expense hesaplari donem net kari olarak Equity'ye yansitilir.
/// </summary>
public interface IBalanceSheetValidationService
{
    /// <summary>
    /// Belirli bir tarih itibariyle bilanco dogrulamasi yapar.
    /// Hesap turlerine gore (Asset, Liability, Equity, Revenue, Expense) siniflama yapar,
    /// Revenue - Expense = Net Kar olarak Equity'ye eklenir.
    /// </summary>
    /// <param name="tenantId">Kiraci kimligi.</param>
    /// <param name="asOfDate">Bilanco tarihi.</param>
    /// <param name="ct">Iptal tokeni.</param>
    /// <returns>Bilanco dogrulama sonucu.</returns>
    Task<BalanceSheetValidationResult> ValidateAsync(
        Guid tenantId,
        DateTime asOfDate,
        CancellationToken ct = default);
}

/// <summary>
/// Bilanco dogrulama sonucu.
/// IsBalanced == true ise Assets == Liabilities + Equity.
/// Difference: Assets - (Liabilities + Equity) — sifir olmasi beklenir.
/// </summary>
public record BalanceSheetValidationResult(
    bool IsBalanced,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal Difference,
    IReadOnlyList<string> Errors);
