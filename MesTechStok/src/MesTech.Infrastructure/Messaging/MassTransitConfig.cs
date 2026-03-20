using MassTransit;
using MesTech.Infrastructure.Messaging.Filters;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using MesTech.Infrastructure.Messaging.Mesa.Consumers;
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
            bus.AddConsumer<MesaMeetingScheduledConsumer>();

            // Muhasebe MESA Consumers (MUH-01 + MUH-02)
            bus.AddConsumer<DocumentClassifiedConsumer>();
            bus.AddConsumer<AccountingApprovalConsumer>();
            bus.AddConsumer<AccountingRejectionConsumer>();
            bus.AddConsumer<AiDocumentExtractedConsumer>();
            bus.AddConsumer<AiReconciliationSuggestedConsumer>();
            bus.AddConsumer<AiAdvisoryRecommendationConsumer>();

            // Bildirim Consumer (İ-13: kayıt eksik düzeltildi)
            bus.AddConsumer<NotificationSentConsumer>();

            // Dalga 9 — On Muhasebe & E-Fatura MESA Consumers
            bus.AddConsumer<AiEInvoiceDraftGeneratedConsumer>();
            bus.AddConsumer<AiErpReconciliationDoneConsumer>();
            bus.AddConsumer<BotEFaturaRequestedConsumer>();

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
                cfg.Message<MesaMeetingScheduledEvent>(x =>
                    x.SetEntityName("mesa.meeting.scheduled"));

                // Muhasebe MESA exchange'ler (MUH-01)
                // MesTech -> MESA (publish)
                cfg.Message<FinanceSettlementImportedEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.settlement.imported.v1"));
                cfg.Message<FinanceDocumentReceivedEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.document.received.v1"));
                // MESA -> MesTech (consume)
                cfg.Message<AiDocumentClassifiedEvent>(x =>
                    x.SetEntityName("mestech.mesa.ai.document.classified.v1"));
                cfg.Message<BotAccountingApprovedEvent>(x =>
                    x.SetEntityName("mestech.mesa.bot.accounting.approved.v1"));
                // MUH-02: MESA -> MesTech (rejection consume)
                cfg.Message<BotAccountingRejectedEvent>(x =>
                    x.SetEntityName("mestech.mesa.bot.accounting.rejected.v1"));
                // MUH-02: MesTech -> MESA (anomaly publish)
                cfg.Message<FinanceAnomalyDetectedEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.anomaly.detected.v1"));

                // MUH-02: MesTech -> MESA (publish — 4 yeni event)
                cfg.Message<FinanceLedgerPostedEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.ledger.posted.v1"));
                cfg.Message<FinanceReconciliationPendingEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.reconciliation.pending.v1"));
                cfg.Message<FinanceBankImportedEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.bank.imported.v1"));
                cfg.Message<FinanceReportDailyEvent>(x =>
                    x.SetEntityName("mestech.mesa.finance.report.daily.v1"));

                // MUH-02: MESA -> MesTech (consume — 3 yeni event)
                cfg.Message<AiDocumentExtractedEvent>(x =>
                    x.SetEntityName("mestech.mesa.ai.document.extracted.v1"));
                cfg.Message<AiReconciliationSuggestedEvent>(x =>
                    x.SetEntityName("mestech.mesa.ai.reconciliation.suggested.v1"));
                cfg.Message<AiAdvisoryRecommendationEvent>(x =>
                    x.SetEntityName("mestech.mesa.ai.advisory.recommendation.v1"));

                // Dalga 9: MESA -> MesTech (consume — e-fatura + ERP uzlastirma)
                cfg.Message<AiEInvoiceDraftGeneratedIntegrationEvent>(x =>
                    x.SetEntityName("mesa.ai.einvoice.draft.generated.v1"));
                cfg.Message<AiErpReconciliationDoneIntegrationEvent>(x =>
                    x.SetEntityName("mesa.ai.erp.reconciliation.done.v1"));
                cfg.Message<BotEFaturaRequestedIntegrationEvent>(x =>
                    x.SetEntityName("mesa.bot.efatura.requested.v1"));

                // İ-13: Idempotency filter — tüm consumer'lara otomatik uygulanır
                cfg.UseConsumeFilter(typeof(IdempotencyFilter<>), context);

                // İ-13: Event version header — tüm outbound mesajlara uygulanır
                cfg.UsePublishFilter(typeof(EventVersionPublishFilter<>), context);

                cfg.ConfigureEndpoints(context);
            });
        });

        // İ-13: Idempotency store DI kaydı
        services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();

        // İ-13: DLQ servisleri
        services.AddScoped<DlqMonitorService>();
        services.AddScoped<DlqReprocessService>();

        // İ-13: MESA event broadcast (SignalR)
        services.AddSingleton<MesaEventBroadcastService>();

        return services;
    }
}
