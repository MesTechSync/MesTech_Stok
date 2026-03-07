using MassTransit;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA OS'tan gelen event'leri consume eder.
/// Dalga 1: Sadece log'a yazar.
/// Dalga 2+: Gercek is mantigi eklenir (Product.Description guncelle, fiyat onerisi kaydet vb.)
/// </summary>
public class MesaAiContentConsumer : IConsumer<MesaAiContentGeneratedEvent>
{
    private readonly ILogger<MesaAiContentConsumer> _logger;

    public MesaAiContentConsumer(ILogger<MesaAiContentConsumer> logger)
    {
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

        // TODO Dalga 2: Product.Description guncelle, SEO metadata kaydet
        return Task.CompletedTask;
    }
}

public class MesaAiPriceConsumer : IConsumer<MesaAiPriceRecommendedEvent>
{
    private readonly ILogger<MesaAiPriceConsumer> _logger;

    public MesaAiPriceConsumer(ILogger<MesaAiPriceConsumer> logger)
    {
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

        // TODO Dalga 2: Fiyat onerisi tablosuna kaydet, UI'da goster
        return Task.CompletedTask;
    }
}

public class MesaBotStatusConsumer : IConsumer<MesaBotNotificationSentEvent>
{
    private readonly ILogger<MesaBotStatusConsumer> _logger;

    public MesaBotStatusConsumer(ILogger<MesaBotStatusConsumer> logger)
    {
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
            _logger.LogWarning(
                "[MESA Consumer] Bot bildirim BASARISIZ: kanal={Channel}, hata={Error}",
                msg.Channel, msg.ErrorMessage);
        }

        // TODO Dalga 2: Bildirim durumunu audit log'a kaydet
        return Task.CompletedTask;
    }
}
