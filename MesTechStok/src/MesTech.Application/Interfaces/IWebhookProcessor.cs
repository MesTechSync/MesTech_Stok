namespace MesTech.Application.Interfaces;

/// <summary>
/// Tum platformlardan gelen webhook'lari isleme pipeline'i.
/// Platform tespiti, imza dogrulama, log kaydi ve event routing yapar.
/// </summary>
public interface IWebhookProcessor
{
    /// <summary>
    /// Webhook payload'ini isler: imza dogrulama → log → event routing.
    /// </summary>
    Task<WebhookResult> ProcessAsync(
        string platform,
        string body,
        string? signature,
        CancellationToken ct);
}

/// <summary>
/// Webhook isleme sonucu.
/// </summary>
public record WebhookResult(
    bool Success,
    string? EventType = null,
    string? Error = null);
