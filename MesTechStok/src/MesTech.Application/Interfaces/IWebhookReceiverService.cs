using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Platformlardan gelen webhook bildirimlerini isleyen servis.
/// Siparis ve iade webhook'lari icin merkezi receiver.
/// </summary>
public interface IWebhookReceiverService
{
    Task<WebhookProcessResult> ProcessOrderWebhookAsync(string platformCode, string payload, CancellationToken ct = default);
    Task<WebhookProcessResult> ProcessClaimWebhookAsync(string platformCode, string payload, CancellationToken ct = default);
    Task<WebhookProcessResult> ProcessGenericWebhookAsync(string platformCode, string eventType, string payload, CancellationToken ct = default);

    /// <summary>
    /// HMAC-SHA256 imza dogrulamali siparis webhook isleyici.
    /// Signature header bos ise ve config'de WebhookSecret tanimli ise istek reddedilir.
    /// </summary>
    Task<WebhookProcessResult> ProcessOrderWebhookAsync(string platformCode, string payload, string? signature, CancellationToken ct = default);

    /// <summary>
    /// HMAC-SHA256 imza dogrulamali iade webhook isleyici.
    /// </summary>
    Task<WebhookProcessResult> ProcessClaimWebhookAsync(string platformCode, string payload, string? signature, CancellationToken ct = default);

    /// <summary>
    /// HMAC-SHA256 imza dogrulamali genel webhook isleyici.
    /// </summary>
    Task<WebhookProcessResult> ProcessGenericWebhookAsync(string platformCode, string eventType, string payload, string? signature, CancellationToken ct = default);
}

public sealed class WebhookProcessResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? EventType { get; set; }
    public string? PlatformOrderId { get; set; }
    public int ProcessedCount { get; set; }
}
