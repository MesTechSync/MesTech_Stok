namespace MesTech.Domain.Interfaces;

/// <summary>
/// Odeme kapisi soyutlamasi — iyzico, Stripe, PayTR gibi provider'lar bu interface'i uygular.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Provider adi (iyzico, Stripe, PayTR).</summary>
    string ProviderName { get; }

    /// <summary>Odeme al — tutar + odeme metodu token'i ile.</summary>
    Task<PaymentResult> ChargeAsync(decimal amount, string currency, string paymentMethodToken,
        string? description = null, CancellationToken ct = default);

    /// <summary>Iade yap — onceki odeme transaction ID ile.</summary>
    Task<PaymentResult> RefundAsync(string transactionId, decimal? partialAmount = null,
        CancellationToken ct = default);

    /// <summary>Kart bilgisini tokenize et (PCI DSS uyumlu).</summary>
    Task<string> SaveCardAsync(CardInfo cardInfo, CancellationToken ct = default);

    /// <summary>Kayitli karti sil.</summary>
    Task<bool> DeleteCardAsync(string cardToken, CancellationToken ct = default);
}
