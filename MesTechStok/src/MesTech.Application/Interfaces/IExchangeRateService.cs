namespace MesTech.Application.Interfaces;

/// <summary>
/// Doviz kuru servisi — TCMB XML API uzerinden guncel kurlar.
/// Dalga 11: Multi-currency destek icin temel arayuz.
/// Cache TTL: 1 saat. Fallback: sabit kurlar (USD=33, EUR=36, GBP=42).
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Belirtilen doviz ciftinin guncel kurunu doner.
    /// </summary>
    /// <param name="fromCurrency">Kaynak para birimi kodu (ISO 4217, orn: "USD", "EUR").</param>
    /// <param name="toCurrency">Hedef para birimi kodu (varsayilan: "TRY").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>1 birim fromCurrency'nin toCurrency cinsinden degeri.</returns>
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency = "TRY", CancellationToken ct = default);

    /// <summary>
    /// Verilen tutari TRY'ye cevirir.
    /// </summary>
    /// <param name="amount">Cevirilecek tutar.</param>
    /// <param name="currency">Kaynak para birimi kodu (ISO 4217).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>TRY cinsinden tutar.</returns>
    Task<decimal> ConvertToTryAsync(decimal amount, string currency, CancellationToken ct = default);

    /// <summary>
    /// Kur cache'ini gecersiz kilar — yeni sorgu TCMB'den cekilir.
    /// </summary>
    void InvalidateCache();
}
