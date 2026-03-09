using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA OS'tan gelen event'leri consume eder.
/// Dalga 1: Sadece log'a yazar.
/// Dalga 2+: Gercek is mantigi eklenir (Product.Description guncelle, fiyat onerisi kaydet vb.)
/// </summary>
public class MesaAiContentConsumer : IConsumer<MesaAiContentGeneratedEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaAiContentConsumer> _logger;

    public MesaAiContentConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaAiContentConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaAiContentGeneratedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] AI icerik alindi: SKU={SKU}, provider={Provider}, icerik uzunlugu={Length}",
            msg.SKU, msg.AiProvider, msg.GeneratedContent.Length);

        if (msg.Metadata is { Count: > 0 })
        {
            _logger.LogDebug(
                "[MESA Consumer] Metadata anahtarlari: {Keys}",
                string.Join(", ", msg.Metadata.Keys));
        }

        _monitor.RecordConsume("ai.content.generated");

        // Dalga 4: Gercek is mantigi — Product.Description guncelle
        // NOT: DbContext inject edilecek, simdilik log yeterli
        _logger.LogInformation(
            "[MESA Consumer] Product aciklamasi guncellenmeli: SKU={SKU}, uzunluk={Length} karakter",
            msg.SKU, msg.GeneratedContent.Length);

        return Task.CompletedTask;
    }
}

public class MesaAiPriceConsumer : IConsumer<MesaAiPriceRecommendedEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaAiPriceConsumer> _logger;

    public MesaAiPriceConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaAiPriceConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaAiPriceRecommendedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] AI fiyat onerisi alindi: SKU={SKU}, oneri={Price}, aralik=[{Min}-{Max}]",
            msg.SKU, msg.RecommendedPrice, msg.MinPrice, msg.MaxPrice);

        if (msg.Reasoning is not null)
        {
            _logger.LogDebug(
                "[MESA Consumer] Fiyat gerekce: {Reasoning}", msg.Reasoning);
        }

        _monitor.RecordConsume("ai.price.recommended");

        // Dalga 4: PriceRecommendation history + Product snapshot
        // NOT: DbContext inject edilecek, simdilik log yeterli
        _logger.LogInformation(
            "[MESA Consumer] Fiyat onerisi kaydedilmeli: SKU={SKU}, oneri={Price:N2} TL, aralik=[{Min:N2}-{Max:N2}]",
            msg.SKU, msg.RecommendedPrice, msg.MinPrice, msg.MaxPrice);

        return Task.CompletedTask;
    }
}

public class MesaBotStatusConsumer : IConsumer<MesaBotNotificationSentEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaBotStatusConsumer> _logger;

    public MesaBotStatusConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaBotStatusConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaBotNotificationSentEvent> context)
    {
        var msg = context.Message;

        if (msg.Success)
        {
            _logger.LogInformation(
                "[MESA Consumer] Bot bildirim basarili: kanal={Channel}, alici={Recipient}",
                msg.Channel, msg.Recipient);
        }
        else
        {
            _logger.LogError(
                "[MESA Consumer] Bot bildirim hatasi — kanal={Channel}, hata={Error}. Ardisik hatalari kontrol edin.",
                msg.Channel, msg.ErrorMessage);
        }

        _monitor.RecordConsume("bot.notification.sent");
        return Task.CompletedTask;
    }
}

public class MesaAiPriceOptimizedConsumer : IConsumer<MesaAiPriceOptimizedEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaAiPriceOptimizedConsumer> _logger;

    public MesaAiPriceOptimizedConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaAiPriceOptimizedConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaAiPriceOptimizedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] AI fiyat optimizasyonu alindi: SKU={SKU}, oneri={Price:N2}, rakip_min={CompMin}, guven={Confidence:P0}",
            msg.SKU, msg.RecommendedPrice, msg.CompetitorMinPrice, msg.Confidence);

        // TODO: PriceRecommendation history INSERT + Product snapshot UPDATE
        // Source = "ai.price.optimized", tek transaction
        // Fiyat farki > %20 ise Telegram Critical alert

        _monitor.RecordConsume("ai.price.optimized");
        return Task.CompletedTask;
    }
}

public class MesaAiStockPredictedConsumer : IConsumer<MesaAiStockPredictedEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaAiStockPredictedConsumer> _logger;

    public MesaAiStockPredictedConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaAiStockPredictedConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaAiStockPredictedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] AI stok tahmini alindi: SKU={SKU}, 7g={D7}, 14g={D14}, 30g={D30}, tukenis={Days} gun",
            msg.SKU, msg.PredictedDemand7d, msg.PredictedDemand14d, msg.PredictedDemand30d, msg.DaysUntilStockout);

        if (msg.DaysUntilStockout < 7)
        {
            _logger.LogWarning(
                "[MESA Consumer] KRITIK: SKU={SKU} {Days} gun icinde tukenecek! Onerilen siparis: {Reorder} adet",
                msg.SKU, msg.DaysUntilStockout, msg.ReorderSuggestion);
        }

        // TODO: StockPrediction history INSERT + Product snapshot UPDATE
        // Tek transaction

        _monitor.RecordConsume("ai.stock.predicted");
        return Task.CompletedTask;
    }
}

public class MesaBotInvoiceRequestConsumer : IConsumer<MesaBotInvoiceRequestedEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaBotInvoiceRequestConsumer> _logger;

    public MesaBotInvoiceRequestConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaBotInvoiceRequestConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaBotInvoiceRequestedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] Musteri fatura istedi: telefon={Phone}, siparis={Order}, kanal={Channel}",
            MesaConsumerHelpers.MaskPhone(msg.CustomerPhone), msg.OrderNumber, msg.RequestChannel);

        // TODO: OrderNumber ile Order bul → Invoice bul → PdfUrl
        // Invoice yoksa: "Faturaniz henuz hazirlanmadi" WhatsApp mesaji
        // Invoice varsa: PdfUrl ile "invoice_ready" template gonder

        _monitor.RecordConsume("bot.invoice.requested");
        return Task.CompletedTask;
    }
}

public class MesaBotReturnRequestConsumer : IConsumer<MesaBotReturnRequestedEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaBotReturnRequestConsumer> _logger;

    public MesaBotReturnRequestConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaBotReturnRequestConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MesaBotReturnRequestedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] Musteri iade istedi: telefon={Phone}, siparis={Order}, sebep={Reason}, kanal={Channel}",
            MesaConsumerHelpers.MaskPhone(msg.CustomerPhone), msg.OrderNumber, msg.ReturnReason, msg.RequestChannel);

        // TODO: OrderNumber ile Order bul → ReturnRequest olustur (line'siz)
        // Status: Initiated, Source: WhatsApp
        // Musteriye: "Iade talebiniz alindi #RET-XXX"
        // Saticiya Telegram: "Musteri X iade talep etti, siparis #Y"

        _monitor.RecordConsume("bot.return.requested");
        return Task.CompletedTask;
    }
}

// ── Shared Helpers ──
file static class MesaConsumerHelpers
{
    internal static string MaskPhone(string phone) =>
        phone.Length > 5
            ? phone[..3] + new string('*', phone.Length - 5) + phone[^2..]
            : "***";
}
