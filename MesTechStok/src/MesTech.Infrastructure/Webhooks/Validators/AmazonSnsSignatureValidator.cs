using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Webhooks.Validators;

/// <summary>
/// Amazon SNS message signature dogrulama.
/// Amazon SP-API webhook'lari SNS uzerinden gelir.
/// Basitlesmis versiyon — sertifika URL domain kontrolu yapar.
/// Production'da tam AWSSDK.SimpleNotificationService kullanilacak.
///
/// Kaynak: https://developer-docs.amazon.com/sp-api/docs/notifications-api-v1-use-case-guide
/// </summary>
public sealed class AmazonSnsSignatureValidator : IWebhookSignatureValidator
{
    public string Platform => "amazon";

    public bool Validate(string body, string signature, string secret)
    {
        // Amazon SNS imza dogrulamasi sertifika-tabanli (RSA).
        // Basitlesmis versiyon: payload icindeki SigningCertURL domain kontrolu.
        try
        {
            var snsMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
            if (snsMessage is null || !snsMessage.ContainsKey("SigningCertURL"))
                return false;

            var certUrl = snsMessage["SigningCertURL"]?.ToString() ?? "";

            // Sertifika URL'si Amazon domain'inden mi?
            if (!certUrl.StartsWith("https://sns.", StringComparison.OrdinalIgnoreCase) ||
                !certUrl.Contains(".amazonaws.com/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Domain dogrulama gecti — tam RSA dogrulama production'da yapilacak
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
