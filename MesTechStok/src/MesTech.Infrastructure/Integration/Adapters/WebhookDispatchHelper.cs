using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Shared webhook dispatch helper — tüm adapter'lar ProcessWebhookPayloadAsync'ten
/// MediatR pipeline'a WebhookReceivedEvent publish etmek için kullanır.
/// IServiceScopeFactory null ise graceful degrade (log-only mode).
/// Replay protection: aynı payload hash'i 5dk içinde tekrar gelirse skip edilir.
/// </summary>
internal static class WebhookDispatchHelper
{
    // Replay protection — SHA256 hash → expiry timestamp
    private static readonly ConcurrentDictionary<string, DateTime> _recentPayloads = new();
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromMinutes(5);
    private static DateTime _lastCleanup = DateTime.UtcNow;

    /// <summary>
    /// Webhook payload'ını parse edip MediatR'a dispatch eder.
    /// Replay protection: aynı platform+payload 5dk içinde tekrar gelirse skip.
    /// </summary>
    public static async Task DispatchAsync(
        IServiceScopeFactory? scopeFactory,
        string platformCode,
        string? eventType,
        string? orderId,
        string? payload,
        ILogger logger,
        CancellationToken ct)
    {
        if (scopeFactory is null)
        {
            logger.LogDebug("{Platform} webhook: IServiceScopeFactory not available — log-only mode", platformCode);
            return;
        }

        // Replay protection — deduplicate identical payloads within 5 minutes
        var payloadHash = ComputePayloadHash(platformCode, payload);
        if (_recentPayloads.TryGetValue(payloadHash, out var lastSeen) && DateTime.UtcNow - lastSeen < ReplayWindow)
        {
            logger.LogWarning("{Platform} webhook REPLAY BLOCKED: duplicate payload within {Window}min. EventType={EventType}",
                platformCode, ReplayWindow.TotalMinutes, eventType);
            return;
        }
        _recentPayloads[payloadHash] = DateTime.UtcNow;

        // Periodic cleanup — remove expired entries every 10 minutes
        if (DateTime.UtcNow - _lastCleanup > TimeSpan.FromMinutes(10))
        {
            _lastCleanup = DateTime.UtcNow;
            var cutoff = DateTime.UtcNow - ReplayWindow;
            foreach (var key in _recentPayloads.Keys)
                if (_recentPayloads.TryGetValue(key, out var ts) && ts < cutoff)
                    _recentPayloads.TryRemove(key, out _);
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>();
            if (mediator is null)
            {
                logger.LogWarning("{Platform} webhook: IMediator not available in DI — not dispatched", platformCode);
                return;
            }

            var notification = new WebhookReceivedEvent(
                PlatformCode: platformCode,
                EventType: eventType ?? "unknown",
                OrderId: orderId,
                RawPayload: payload ?? string.Empty,
                ReceivedAt: DateTime.UtcNow);

            await mediator.Publish(notification, ct).ConfigureAwait(false);

            logger.LogInformation("{Platform} webhook dispatched: EventType={EventType} OrderId={OrderId}",
                platformCode, eventType, orderId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Platform} webhook dispatch failed", platformCode);
        }
    }

    private static string ComputePayloadHash(string platformCode, string? payload)
    {
        var input = $"{platformCode}:{payload ?? string.Empty}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }
}
