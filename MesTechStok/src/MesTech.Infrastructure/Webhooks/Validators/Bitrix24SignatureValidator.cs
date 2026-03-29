using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// Bitrix24 webhook imza dogrulama — application_token karsilastirmasi.
/// Bitrix24 CRM webhook'lari form-encoded gelir: auth[application_token]=xxx
/// HMAC yerine sabit token karsilastirmasi yapilir (Bitrix24 dokumantasyonu).
/// </summary>
public sealed class Bitrix24SignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "bitrix24";

    public bool Validate(string body, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        // Timing-safe comparison to prevent timing attacks on token value
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature.Trim()),
            Encoding.UTF8.GetBytes(secret.Trim()));
    }
}
