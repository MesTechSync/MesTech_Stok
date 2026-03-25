using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// Zalando webhook imza dogrulama — HMAC-SHA256 hex.
/// X-Zalando-Signature header'i ile dogrulanir.
/// </summary>
public sealed class ZalandoSignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "zalando";

    public bool Validate(string body, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
