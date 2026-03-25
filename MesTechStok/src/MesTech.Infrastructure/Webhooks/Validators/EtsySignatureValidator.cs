using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// Etsy webhook imza dogrulama — HMAC-SHA256 base64.
/// X-Etsy-Signature header'i ile dogrulanir.
/// Etsy Open API v3: base64-encoded HMAC-SHA256.
/// </summary>
public sealed class EtsySignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "etsy";

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
