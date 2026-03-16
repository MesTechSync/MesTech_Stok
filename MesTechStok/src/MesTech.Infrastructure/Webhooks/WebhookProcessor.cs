using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Webhooks;

/// <summary>
/// Merkezi webhook isleme servisi.
/// Imza dogrulama → WebhookLog kaydi → event routing pipeline'i.
/// </summary>
public class WebhookProcessor : IWebhookProcessor
{
    private readonly IEnumerable<IWebhookSignatureValidator> _validators;
    private readonly WebhookEventRouter _router;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookProcessor> _logger;

    public WebhookProcessor(
        IEnumerable<IWebhookSignatureValidator> validators,
        WebhookEventRouter router,
        ITenantProvider tenantProvider,
        IConfiguration configuration,
        ILogger<WebhookProcessor> logger)
    {
        _validators = validators;
        _router = router;
        _tenantProvider = tenantProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WebhookResult> ProcessAsync(
        string platform,
        string body,
        string? signature,
        CancellationToken ct)
    {
        var normalizedPlatform = platform.ToLowerInvariant();

        _logger.LogInformation(
            "Webhook received: platform={Platform}, bodyLength={BodyLength}, hasSignature={HasSignature}",
            normalizedPlatform, body.Length, signature is not null);

        // 1. Imza dogrulama
        var validator = _validators.FirstOrDefault(
            v => v.Platform.Equals(normalizedPlatform, StringComparison.OrdinalIgnoreCase));

        var isValid = true;
        if (validator is not null && signature is not null)
        {
            var secret = _configuration[$"Webhooks:Secrets:{normalizedPlatform}"] ?? string.Empty;
            isValid = validator.Validate(body, signature, secret);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Webhook signature validation FAILED: platform={Platform}",
                    normalizedPlatform);

                return new WebhookResult(false, Error: "Invalid webhook signature");
            }
        }
        else if (validator is not null && signature is null)
        {
            _logger.LogWarning(
                "Webhook received without signature: platform={Platform}",
                normalizedPlatform);
            // Accept but log — some platforms may not always send signatures
        }

        // 2. Event type cikarimi
        var eventType = ExtractEventType(body, normalizedPlatform);

        _logger.LogInformation(
            "Webhook event type extracted: platform={Platform}, eventType={EventType}",
            normalizedPlatform, eventType);

        // 3. Event routing
        try
        {
            var routedType = await _router.RouteAsync(
                normalizedPlatform, eventType, body, ct);

            _logger.LogInformation(
                "Webhook processed successfully: platform={Platform}, eventType={EventType}, routed={RoutedType}",
                normalizedPlatform, eventType, routedType ?? "none");

            return new WebhookResult(true, routedType ?? eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Webhook processing FAILED: platform={Platform}, eventType={EventType}",
                normalizedPlatform, eventType);

            return new WebhookResult(false, eventType, ex.Message);
        }
    }

    /// <summary>
    /// JSON payload'dan event type bilgisini cikarir.
    /// Farkli platformlar farkli field isimleri kullanir.
    /// </summary>
    private static string ExtractEventType(string body, string platform)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Try common event type field names
            foreach (var field in new[] { "event", "event_type", "eventType", "type", "topic", "webhook_topic" })
            {
                if (root.TryGetProperty(field, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString() ?? "unknown";
            }

            // Platform-specific fallbacks
            if (root.TryGetProperty("resource", out var resource) &&
                resource.ValueKind == JsonValueKind.String)
                return resource.GetString() ?? "unknown";
        }
        catch
        {
            // Non-JSON body or parse error
        }

        return "unknown";
    }
}
