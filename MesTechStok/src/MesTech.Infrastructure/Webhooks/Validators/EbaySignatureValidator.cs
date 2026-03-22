using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// eBay marketplace webhook imza dogrulama — SHA256 HMAC base64.
/// Header: X-eBay-Signature
/// eBay verification token ile HMAC hesaplanir.
/// Kaynak: https://developer.ebay.com/develop/apis/marketplace-account-deletion
/// </summary>
public sealed class EbaySignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "ebay";

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
