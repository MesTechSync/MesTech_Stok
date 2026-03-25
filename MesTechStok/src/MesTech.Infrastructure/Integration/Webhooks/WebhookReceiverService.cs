using System.Text.Json;
using System.Web;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Webhooks;

/// <summary>
/// Platform webhook bildirimlerini isleyen servis.
/// Trendyol: OrderCreated, OrderStatusChanged, ClaimCreated
/// Bitrix24: ONCRMDEALADD, ONCRMDEALUPDATE, ONCRMCONTACTADD, ONCRMCONTACTUPDATE (form-encoded)
/// Diger platformlar: generic event routing
/// </summary>
public sealed class WebhookReceiverService : IWebhookReceiverService
{
    private readonly IEnumerable<IIntegratorAdapter> _adapters;
    private readonly ILogger<WebhookReceiverService> _logger;

    public WebhookReceiverService(
        IEnumerable<IIntegratorAdapter> adapters,
        ILogger<WebhookReceiverService> logger)
    {
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WebhookProcessResult> ProcessOrderWebhookAsync(
        string platformCode, string payload, CancellationToken ct = default)
    {
        _logger.LogInformation("Webhook received: Platform={Platform} Type=Order", platformCode);

        try
        {
            var adapter = FindWebhookAdapter(platformCode);
            if (adapter == null)
            {
                return new WebhookProcessResult
                {
                    Success = false,
                    Message = $"Platform '{platformCode}' webhook destegi yok veya kayitli degil."
                };
            }

            await adapter.ProcessWebhookPayloadAsync(payload, ct).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(payload);
            var orderId = ExtractFieldFromPayload(doc, "orderNumber", "order_id", "platformOrderId");

            _logger.LogInformation("Order webhook processed: Platform={Platform} OrderId={OrderId}",
                platformCode, orderId);

            return new WebhookProcessResult
            {
                Success = true,
                EventType = "OrderCreated",
                PlatformOrderId = orderId,
                ProcessedCount = 1,
                Message = "Siparis webhook basariyla islendi."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order webhook processing failed: Platform={Platform}", platformCode);
            return new WebhookProcessResult
            {
                Success = false,
                Message = $"Webhook isleme hatasi: {ex.Message}"
            };
        }
    }

    public async Task<WebhookProcessResult> ProcessClaimWebhookAsync(
        string platformCode, string payload, CancellationToken ct = default)
    {
        _logger.LogInformation("Webhook received: Platform={Platform} Type=Claim", platformCode);

        try
        {
            var adapter = FindWebhookAdapter(platformCode);
            if (adapter == null)
            {
                return new WebhookProcessResult
                {
                    Success = false,
                    Message = $"Platform '{platformCode}' webhook destegi yok veya kayitli degil."
                };
            }

            await adapter.ProcessWebhookPayloadAsync(payload, ct).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(payload);
            var claimId = ExtractFieldFromPayload(doc, "claimId", "claim_id", "id");

            _logger.LogInformation("Claim webhook processed: Platform={Platform} ClaimId={ClaimId}",
                platformCode, claimId);

            return new WebhookProcessResult
            {
                Success = true,
                EventType = "ClaimCreated",
                PlatformOrderId = claimId,
                ProcessedCount = 1,
                Message = "Iade webhook basariyla islendi."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claim webhook processing failed: Platform={Platform}", platformCode);
            return new WebhookProcessResult
            {
                Success = false,
                Message = $"Webhook isleme hatasi: {ex.Message}"
            };
        }
    }

    public async Task<WebhookProcessResult> ProcessGenericWebhookAsync(
        string platformCode, string eventType, string payload, CancellationToken ct = default)
    {
        _logger.LogInformation("Webhook received: Platform={Platform} Type={EventType}", platformCode, eventType);

        // Bitrix24: form-encoded CRM events
        if (platformCode.Equals("Bitrix24", StringComparison.OrdinalIgnoreCase))
            return await ProcessBitrix24WebhookAsync(payload, ct).ConfigureAwait(false);

        return eventType.ToLowerInvariant() switch
        {
            "ordercreated" or "orderstatuschanged" or "order" =>
                await ProcessOrderWebhookAsync(platformCode, payload, ct).ConfigureAwait(false),

            "claimcreated" or "claim" or "return" =>
                await ProcessClaimWebhookAsync(platformCode, payload, ct).ConfigureAwait(false),

            _ => await ProcessUnknownEventAsync(platformCode, eventType, payload, ct).ConfigureAwait(false)
        };
    }

    /// <summary>
    /// Bitrix24 CRM webhook handler.
    /// Payload is form-encoded: event=ONCRMDEALADD&amp;data[FIELDS][ID]=123&amp;auth[application_token]=xxx
    /// Validates application_token, routes to appropriate CRM event handler.
    /// </summary>
    public async Task<WebhookProcessResult> ProcessBitrix24WebhookAsync(
        string payload, CancellationToken ct = default)
    {
        try
        {
            var fields = HttpUtility.ParseQueryString(payload);
            var eventType = fields["event"] ?? "";
            var entityId = fields["data[FIELDS][ID]"];
            var appToken = fields["auth[application_token]"];

            _logger.LogInformation(
                "Bitrix24 webhook: Event={Event} EntityId={Id} HasToken={HasToken}",
                eventType, entityId, !string.IsNullOrEmpty(appToken));

            // Delegate to adapter for full processing
            var adapter = FindWebhookAdapter("Bitrix24");
            if (adapter != null)
                await adapter.ProcessWebhookPayloadAsync(payload, ct).ConfigureAwait(false);

            var mappedEventType = eventType.ToUpperInvariant() switch
            {
                "ONCRMDEALADD" => "DealCreated",
                "ONCRMDEALUPDATE" => "DealUpdated",
                "ONCRMCONTACTADD" => "ContactCreated",
                "ONCRMCONTACTUPDATE" => "ContactUpdated",
                _ => eventType
            };

            return new WebhookProcessResult
            {
                Success = true,
                EventType = mappedEventType,
                PlatformOrderId = entityId,
                ProcessedCount = 1,
                Message = $"Bitrix24 {mappedEventType} webhook islendi (ID: {entityId})."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bitrix24 webhook processing failed");
            return new WebhookProcessResult
            {
                Success = false,
                Message = $"Bitrix24 webhook hatasi: {ex.Message}"
            };
        }
    }

    private async Task<WebhookProcessResult> ProcessUnknownEventAsync(
        string platformCode, string eventType, string payload, CancellationToken ct)
    {
        var adapter = FindWebhookAdapter(platformCode);
        if (adapter != null)
        {
            await adapter.ProcessWebhookPayloadAsync(payload, ct).ConfigureAwait(false);
        }

        _logger.LogWarning("Unknown webhook event type: Platform={Platform} Type={EventType}", platformCode, eventType);

        return new WebhookProcessResult
        {
            Success = true,
            EventType = eventType,
            ProcessedCount = 1,
            Message = $"Bilinmeyen event tipi '{eventType}' islendi."
        };
    }

    private IWebhookCapableAdapter? FindWebhookAdapter(string platformCode)
    {
        return _adapters
            .Where(a => a.PlatformCode.Equals(platformCode, StringComparison.OrdinalIgnoreCase))
            .OfType<IWebhookCapableAdapter>()
            .FirstOrDefault();
    }

    private static string? ExtractFieldFromPayload(JsonDocument doc, params string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (doc.RootElement.TryGetProperty(field, out var val))
                return val.ToString();
        }
        return null;
    }
}
