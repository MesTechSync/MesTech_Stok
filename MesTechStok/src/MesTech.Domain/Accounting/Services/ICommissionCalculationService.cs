namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Platform komisyon hesaplama servisi arayuzu.
/// </summary>
public interface ICommissionCalculationService
{
    /// <summary>
    /// Brut tutar uzerinden komisyon tutarini hesaplar (senkron — fallback oranlar).
    /// </summary>
    decimal CalculateCommission(string platform, string? category, decimal grossAmount);

    /// <summary>
    /// Brut tutar uzerinden komisyon tutarini hesaplar (asenkron — dinamik oran destegi).
    /// Dinamik oran bulunamazsa fallback kullanir.
    /// </summary>
    Task<CommissionCalculationResult> CalculateCommissionAsync(
        string platform,
        string? category,
        decimal grossAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Platform bazinda varsayilan komisyon oranini dondurur.
    /// </summary>
    decimal GetDefaultRate(string platform);
}
