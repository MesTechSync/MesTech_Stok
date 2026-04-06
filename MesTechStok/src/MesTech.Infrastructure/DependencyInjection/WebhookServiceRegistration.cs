using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Integration.Orchestration;
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
        // === Webhook Signature Validators (14 platform) ===
        services.AddSingleton<IWebhookSignatureValidator, TrendyolSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, ShopifySignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, WooCommerceSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, HepsiburadaSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, AmazonSnsSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, CiceksepetiSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, EbaySignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, N11SignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, OzonSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, PttAvmSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, PazaramaSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, EtsySignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, ZalandoSignatureValidator>();
        services.AddSingleton<IWebhookSignatureValidator, Bitrix24SignatureValidator>();

        // === Webhook Processing Pipeline ===
        services.AddScoped<WebhookEventRouter>();
        services.AddScoped<IWebhookProcessor, WebhookProcessor>();

        // === WebhookReceivedEvent Handler (MediatR — tüm platformlar) ===
        services.AddScoped<INotificationHandler<MesTech.Infrastructure.Messaging.WebhookReceivedEvent>,
            MesTech.Infrastructure.Integration.Webhooks.WebhookReceivedEventHandler>();

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

        // === Orphan Event Bridge Handlers ===
        services.AddScoped<INotificationHandler<DomainEventNotification<OrderCompletedEvent>>,
            OrderCompletedEventHandler>();
        services.AddScoped<INotificationHandler<DomainEventNotification<CustomerCreatedEvent>>,
            CustomerCreatedEventHandler>();

        return services;
    }
}
