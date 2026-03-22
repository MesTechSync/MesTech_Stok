using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// Ciceksepeti webhook imza dogrulama — SHA256 HMAC hex.
/// Header: X-CS-Signature
/// Ciceksepeti Partner API webhook security dokumantasyonuna uygun.
/// </summary>
public sealed class CiceksepetiSignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "ciceksepeti";

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
