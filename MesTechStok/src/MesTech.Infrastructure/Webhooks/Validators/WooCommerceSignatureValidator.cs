using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// WooCommerce webhook imza dogrulama — HMAC-SHA256 Base64.
/// X-WC-Webhook-Signature header'i ile dogrulanir.
/// </summary>
public sealed class WooCommerceSignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "woocommerce";

    public bool Validate(string body, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        var computed = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }
}
