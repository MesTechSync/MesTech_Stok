using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Webhooks;

/// <summary>
/// Webhook endpoint'leri icin minimal API kayit sinifi.
/// WebApplication iceren bir projede MapWebhookEndpoints() ile kullanilir.
///
/// Ornek kullanim:
///   app.MapWebhookEndpoints();
///
/// Endpoint'ler:
///   POST /api/webhooks/{platformCode}/orders   → siparis bildirimi
///   POST /api/webhooks/{platformCode}/claims   → iade bildirimi
///   POST /api/webhooks/{platformCode}/{event}  → genel bildirim
/// </summary>
public static class WebhookEndpoints
{
    /// <summary>
    /// Webhook alici endpoint'lerini kaydeder.
    /// ASP.NET Minimal API veya self-hosted Kestrel ile kullanilir.
    /// Bu metod Microsoft.AspNetCore.App framework referansi gerektirir;
    /// WPF Desktop projesinde dogrudan cagirilmaz.
    /// </summary>
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IWebhookReceiverService, WebhookReceiverService>();
    }

    /// <summary>
    /// Webhook payload'inin HMAC-SHA256 ile dogrulanmasi.
    /// Trendyol ve diger platformlar icin guvenlik katmani.
    /// </summary>
    public static bool ValidateHmacSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }
}

/// <summary>
/// Webhook request DTO — platform webhook cagirildiginda gelen istek modeli.
/// Minimal API endpoint handler'larinda bind edilir.
/// </summary>
public class WebhookRequest
{
    public string? EventType { get; set; }
    public string? Signature { get; set; }
    public string Payload { get; set; } = string.Empty;
}
