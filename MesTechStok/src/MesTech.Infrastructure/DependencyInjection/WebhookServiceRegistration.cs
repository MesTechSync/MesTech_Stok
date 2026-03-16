using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Services;
using MesTech.Infrastructure.Webhooks;
using MesTech.Infrastructure.Webhooks.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.DependencyInjection;

/// <summary>
/// Webhook altyapisi ve SignalR real-time bildirim servisleri DI kayitlari.
/// G-01: Webhook Receiver + G-02: SignalR Real-Time.
/// </summary>
public static class WebhookServiceRegistration
{
    public static IServiceCollection AddWebhookServices(this IServiceCollection services)
    {
        // === Webhook Signature Validators ===
        services.AddSingleton<IWebhookSignatureValidator, TrendyolSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, ShopifySignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, WooCommerceSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, HepsiburadaSignatureValidator>();

        // === Webhook Processing Pipeline ===
        services.AddScoped<WebhookEventRouter>();
        services.AddScoped<IWebhookProcessor, WebhookProcessor>();

        // === Webhook Retry Job ===
        services.AddScoped<Jobs.WebhookRetryJob>();

        // === SignalR Notification Bridge (MediatR handlers) ===
        services.AddScoped<INotificationHandler<DomainEventNotification<OrderReceivedEvent>>,
            SignalRNotificationBridge>();
        services.AddScoped<INotificationHandler<DomainEventNotification<LowStockDetectedEvent>>,
            SignalRNotificationBridge>();
        services.AddScoped<INotificationHandler<DomainEventNotification<InvoiceCreatedEvent>>,
            SignalRNotificationBridge>();
        services.AddScoped<INotificationHandler<DomainEventNotification<OrderCancelledEvent>>,
            SignalRNotificationBridge>();

        return services;
    }
}
