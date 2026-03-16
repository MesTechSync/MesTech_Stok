namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform-specific webhook imza dogrulama interface'i.
/// Her platform kendi HMAC/Hash stratejisini implement eder.
/// </summary>
public interface IWebhookSignatureValidator
{
    /// <summary>Platform kodu (trendyol, shopify, woocommerce, hepsiburada).</summary>
    string Platform { get; }

    /// <summary>
    /// Webhook body ve imzayi, platform secret ile dogrular.
    /// </summary>
    bool Validate(string body, string signature, string secret);
}
