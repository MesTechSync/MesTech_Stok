using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Shared webhook dispatch helper — tüm adapter'lar ProcessWebhookPayloadAsync'ten
/// MediatR pipeline'a WebhookReceivedEvent publish etmek için kullanır.
/// IServiceScopeFactory null ise graceful degrade (log-only mode).
/// </summary>
internal static class WebhookDispatchHelper
{
    /// <summary>
    /// Webhook payload'ını parse edip MediatR'a dispatch eder.
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
}
