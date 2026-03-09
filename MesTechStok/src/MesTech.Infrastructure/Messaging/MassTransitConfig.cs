using MassTransit;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Messaging;

public static class MassTransitConfig
{
    public static IServiceCollection AddMesTechMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("RabbitMQ:Username is not configured. Set it via appsettings or user-secrets.");
        var rabbitPass = configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException("RabbitMQ:Password is not configured. Set it via appsettings or user-secrets.");
        ushort rabbitPort = 5672;
        if (ushort.TryParse(configuration["RabbitMQ:Port"], out var parsedPort))
            rabbitPort = parsedPort;

        services.AddMassTransit(bus =>
        {
            // MESA OS Consumer'lar
            bus.AddConsumer<MesaAiContentConsumer>();
            bus.AddConsumer<MesaAiPriceConsumer>();
            bus.AddConsumer<MesaBotStatusConsumer>();
            bus.AddConsumer<MesaAiPriceOptimizedConsumer>();
            bus.AddConsumer<MesaAiStockPredictedConsumer>();
            bus.AddConsumer<MesaBotInvoiceRequestConsumer>();
            bus.AddConsumer<MesaBotReturnRequestConsumer>();
            bus.AddConsumer<MesaDlqConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, rabbitPort, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                // Mevcut exchange'ler (DOKUNULMAZ)
                cfg.Message<StockChangedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.stock.changed"));
                cfg.Message<PriceChangedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.price.changed"));
                cfg.Message<OrderReceivedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.order.received"));
                cfg.Message<InvoiceCreatedIntegrationEvent>(x =>
                    x.SetEntityName("mestech.invoice.created"));

                // MESA OS exchange'ler — MesTech -> MESA
                cfg.Message<MesaProductCreatedEvent>(x =>
                    x.SetEntityName("mestech.mesa.product.created"));
                cfg.Message<MesaStockLowEvent>(x =>
                    x.SetEntityName("mestech.mesa.stock.low"));
                cfg.Message<MesaOrderReceivedEvent>(x =>
                    x.SetEntityName("mestech.mesa.order.received"));
                cfg.Message<MesaPriceChangedEvent>(x =>
                    x.SetEntityName("mestech.mesa.price.changed"));
                cfg.Message<MesaInvoiceGeneratedEvent>(x =>
                    x.SetEntityName("mestech.mesa.invoice.generated"));
                cfg.Message<MesaInvoiceCancelledEvent>(x =>
                    x.SetEntityName("mestech.mesa.invoice.cancelled"));
                cfg.Message<MesaReturnCreatedEvent>(x =>
                    x.SetEntityName("mestech.mesa.return.created"));
                cfg.Message<MesaReturnResolvedEvent>(x =>
                    x.SetEntityName("mestech.mesa.return.resolved"));
                cfg.Message<MesaBuyboxLostEvent>(x =>
                    x.SetEntityName("mestech.mesa.buybox.lost"));
                cfg.Message<MesaSupplierFeedSyncedEvent>(x =>
                    x.SetEntityName("mestech.mesa.supplier.feed.synced"));

                // MESA OS exchange'ler — MESA -> MesTech
                cfg.Message<MesaAiContentGeneratedEvent>(x =>
                    x.SetEntityName("mesa.ai.content.generated"));
                cfg.Message<MesaAiPriceRecommendedEvent>(x =>
                    x.SetEntityName("mesa.ai.price.recommended"));
                cfg.Message<MesaBotNotificationSentEvent>(x =>
                    x.SetEntityName("mesa.bot.notification.sent"));
                cfg.Message<MesaAiPriceOptimizedEvent>(x =>
                    x.SetEntityName("mesa.ai.price.optimized"));
                cfg.Message<MesaAiStockPredictedEvent>(x =>
                    x.SetEntityName("mesa.ai.stock.predicted"));
                cfg.Message<MesaBotInvoiceRequestedEvent>(x =>
                    x.SetEntityName("mesa.bot.invoice.requested"));
                cfg.Message<MesaBotReturnRequestedEvent>(x =>
                    x.SetEntityName("mesa.bot.return.requested"));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
