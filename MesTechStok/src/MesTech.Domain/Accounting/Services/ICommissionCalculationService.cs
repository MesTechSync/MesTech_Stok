namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Platform komisyon hesaplama servisi arayuzu.
/// </summary>
public interface ICommissionCalculationService
{
    /// <summary>
    /// Brut tutar uzerinden komisyon tutarini hesaplar.
    /// </summary>
    decimal CalculateCommission(string platform, string? category, decimal grossAmount);

    /// <summary>
    /// Platform bazinda varsayilan komisyon oranini dondurur.
    /// </summary>
    decimal GetDefaultRate(string platform);
}
