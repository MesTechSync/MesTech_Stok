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
        // Bitrix24 uses application_token comparison, not HMAC.
        // The "signature" parameter here is the application_token from the webhook payload.
        // The "secret" is the expected application_token from configuration.
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        return string.Equals(signature.Trim(), secret.Trim(), StringComparison.Ordinal);
    }
}
