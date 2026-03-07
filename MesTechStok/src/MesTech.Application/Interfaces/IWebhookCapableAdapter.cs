namespace MesTech.Application.Interfaces;

/// <summary>
/// Webhook desteği olan platform adaptörleri için opsiyonel interface.
/// </summary>
public interface IWebhookCapableAdapter
{
    Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default);
    Task<bool> UnregisterWebhookAsync(CancellationToken ct = default);
    Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default);
}
