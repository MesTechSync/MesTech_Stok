using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;

public sealed class ProcessPaymentWebhookHandler
    : IRequestHandler<ProcessPaymentWebhookCommand, PaymentWebhookResult>
{
    private readonly ITenantSubscriptionRepository _subscriptionRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProcessPaymentWebhookHandler> _logger;
    private readonly IPaymentWebhookSecretProvider _secretProvider;

    public ProcessPaymentWebhookHandler(
        ITenantSubscriptionRepository subscriptionRepo,
        IUnitOfWork uow,
        ILogger<ProcessPaymentWebhookHandler> logger,
        IPaymentWebhookSecretProvider secretProvider)
    {
        _subscriptionRepo = subscriptionRepo;
        _uow = uow;
        _logger = logger;
        _secretProvider = secretProvider;
    }

    public async Task<PaymentWebhookResult> Handle(
        ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment webhook from {Provider}", request.Provider);

        // 1. Signature verification
        if (!VerifySignature(request.Provider, request.RawBody, request.Signature))
        {
            _logger.LogWarning("Invalid webhook signature from {Provider}", request.Provider);
            return PaymentWebhookResult.Fail("Invalid signature");
        }

        // 2. Parse event
        string eventType;
        Guid? subscriptionId = null;

        try
        {
            using var doc = JsonDocument.Parse(request.RawBody);
            var root = doc.RootElement;

            eventType = request.Provider.Equals("stripe", StringComparison.OrdinalIgnoreCase)
                ? root.GetProperty("type").GetString() ?? "unknown"
                : root.TryGetProperty("event_type", out var et)
                    ? et.GetString() ?? "unknown"
                    : "unknown";

            // Extract subscription info from metadata
            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("object", out var obj) &&
                obj.TryGetProperty("metadata", out var meta) &&
                meta.TryGetProperty("subscription_id", out var subId))
            {
                if (Guid.TryParse(subId.GetString(), out var parsedId))
                    subscriptionId = parsedId;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse webhook body from {Provider}", request.Provider);
            return PaymentWebhookResult.Fail("Invalid JSON body");
        }

        // 3. Handle event
        _logger.LogInformation("Webhook event: {EventType}, SubscriptionId: {SubscriptionId}",
            eventType, subscriptionId);

        switch (eventType)
        {
            case "payment_intent.succeeded":
            case "invoice.paid":
                await HandlePaymentSuccessAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                break;

            case "invoice.payment_failed":
            case "payment_intent.payment_failed":
                await HandlePaymentFailedAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                break;

            default:
                _logger.LogDebug("Unhandled webhook event type: {EventType}", eventType);
                break;
        }

        return PaymentWebhookResult.Ok(eventType, subscriptionId);
    }

    private async Task HandlePaymentSuccessAsync(Guid? subscriptionId, CancellationToken ct)
    {
        if (!subscriptionId.HasValue) return;
        var sub = await _subscriptionRepo.GetByIdAsync(subscriptionId.Value, ct).ConfigureAwait(false);
        if (sub is null) return;
        sub.Renew();
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Subscription {Id} renewed via payment webhook", subscriptionId);
    }

    private async Task HandlePaymentFailedAsync(Guid? subscriptionId, CancellationToken ct)
    {
        if (!subscriptionId.HasValue) return;
        var sub = await _subscriptionRepo.GetByIdAsync(subscriptionId.Value, ct).ConfigureAwait(false);
        if (sub is null) return;
        sub.MarkPastDue();
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Subscription {Id} marked past due via payment webhook", subscriptionId);
    }

    private async Task HandleSubscriptionDeletedAsync(Guid? subscriptionId, CancellationToken ct)
    {
        if (!subscriptionId.HasValue) return;
        var sub = await _subscriptionRepo.GetByIdAsync(subscriptionId.Value, ct).ConfigureAwait(false);
        if (sub is null) return;
        sub.Cancel("Cancelled via payment provider");
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Subscription {Id} cancelled via payment webhook", subscriptionId);
    }

    private bool VerifySignature(string provider, string body, string? signature)
    {
        var secret = _secretProvider.GetSecret(provider);

        // GÜVENLİK: Secret yoksa REJECT — sandbox bile olsa imza doğrulaması ZORUNLU
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError(
                "Webhook REJECTED — no secret configured for provider {Provider}. " +
                "Configure webhook secret via user-secrets or environment variable.", provider);
            return false;
        }

        if (string.IsNullOrWhiteSpace(signature))
            return false;

        if (provider.Equals("stripe", StringComparison.OrdinalIgnoreCase))
            return VerifyStripeSignature(body, signature, secret);

        if (provider.Equals("iyzico", StringComparison.OrdinalIgnoreCase))
            return VerifyIyzicoSignature(body, signature, secret);

        if (provider.Equals("paytr", StringComparison.OrdinalIgnoreCase))
            return VerifyHmacSha256(body, signature, secret);

        _logger.LogWarning("No signature verifier for provider {Provider} — rejecting", provider);
        return false;
    }

    private static bool VerifyStripeSignature(string body, string signature, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        // Stripe signature format: t=timestamp,v1=hash
        string? timestamp = null;
        string? hash = null;

        foreach (var part in signature.Split(','))
        {
            if (part.StartsWith("t=", StringComparison.Ordinal))
                timestamp = part[2..];
            else if (part.StartsWith("v1=", StringComparison.Ordinal))
                hash = part[3..];
        }

        if (timestamp is null || hash is null)
            return false;

        var payload = $"{timestamp}.{body}";
        var expectedHash = ComputeHmacSha256(payload, secret!);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(expectedHash));
    }

    /// <summary>
    /// iyzico webhook: HMAC-SHA256 of raw body with webhook secret.
    /// iyzico sends signature in X-IYZ-Signature header (base64).
    /// </summary>
    private bool VerifyIyzicoSignature(string body, string signature, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Iyzico webhook REJECTED — no secret configured");
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var computed = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }

    /// <summary>Generic HMAC-SHA256 hex verification (PayTR etc.).</summary>
    private static bool VerifyHmacSha256(string body, string signature, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        var computed = ComputeHmacSha256(body, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hashBytes);
    }
}
