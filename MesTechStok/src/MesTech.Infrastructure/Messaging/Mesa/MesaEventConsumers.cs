using MassTransit;
using MediatR;
using MesTech.Application.Commands.ApplyOptimizedPrice;
using MesTech.Application.Commands.ProcessBotInvoiceRequest;
using MesTech.Application.Commands.ProcessBotReturnRequest;
using MesTech.Application.Commands.UpdateBotNotificationStatus;
using MesTech.Application.Commands.UpdateProductContent;
using MesTech.Application.Commands.UpdateProductPrice;
using MesTech.Application.Commands.UpdateStockForecast;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA OS'tan gelen event'leri consume eder.
/// Dalga 1: Sadece log'a yazar.
/// Dalga 2+: Gercek is mantigi eklenir (Product.Description guncelle, fiyat onerisi kaydet vb.)
/// Dalga 5 IP-5: TenantId eklendi — fallback: ITenantProvider.
/// I-13 S-02: 7 consumer deepened — domain logic on top of existing logs.
/// G197: Inline entity creation moved to CQRS command handlers.
/// </summary>
public sealed class MesaAiContentConsumer : IConsumer<MesaAiContentGeneratedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaAiContentConsumer> _logger;

    public MesaAiContentConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaAiContentConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaAiContentGeneratedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaAiContentGeneratedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new UpdateProductContentCommand
            {
                ProductId = msg.ProductId,
                SKU = msg.SKU,
                GeneratedContent = msg.GeneratedContent,
                AiProvider = msg.AiProvider,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaAiContentGeneratedEvent));
            throw; // Let MassTransit retry policy handle
        }

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
    }
}

public sealed class MesaAiPriceConsumer : IConsumer<MesaAiPriceRecommendedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaAiPriceConsumer> _logger;

    public MesaAiPriceConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaAiPriceConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaAiPriceRecommendedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaAiPriceRecommendedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new UpdateProductPriceCommand
            {
                ProductId = msg.ProductId,
                SKU = msg.SKU,
                RecommendedPrice = msg.RecommendedPrice,
                MinPrice = msg.MinPrice,
                MaxPrice = msg.MaxPrice,
                Reasoning = msg.Reasoning,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaAiPriceRecommendedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI fiyat onerisi alindi: SKU={SKU}, oneri={Price}, aralik=[{Min}-{Max}]",
            msg.SKU, msg.RecommendedPrice, msg.MinPrice, msg.MaxPrice);

        if (msg.Reasoning is not null)
        {
            _logger.LogDebug(
                "[MESA Consumer] Fiyat gerekce: {Reasoning}", msg.Reasoning);
        }

        _monitor.RecordConsume("ai.price.recommended");
    }
}

public sealed class MesaBotStatusConsumer : IConsumer<MesaBotNotificationSentEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaBotStatusConsumer> _logger;

    public MesaBotStatusConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaBotStatusConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaBotNotificationSentEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaBotNotificationSentEvent), context.MessageId);

        try
        {
            await _mediator.Send(new UpdateBotNotificationStatusCommand
            {
                Channel = msg.Channel,
                Recipient = msg.Recipient,
                Success = msg.Success,
                ErrorMessage = msg.ErrorMessage,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaBotNotificationSentEvent));
            throw; // Let MassTransit retry policy handle
        }

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
    }
}

public sealed class MesaAiPriceOptimizedConsumer : IConsumer<MesaAiPriceOptimizedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaAiPriceOptimizedConsumer> _logger;

    public MesaAiPriceOptimizedConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaAiPriceOptimizedConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaAiPriceOptimizedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaAiPriceOptimizedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new ApplyOptimizedPriceCommand
            {
                ProductId = msg.ProductId,
                SKU = msg.SKU,
                RecommendedPrice = msg.RecommendedPrice,
                MinPrice = msg.MinPrice,
                MaxPrice = msg.MaxPrice,
                CompetitorMinPrice = msg.CompetitorMinPrice,
                Confidence = msg.Confidence,
                Reasoning = msg.Reasoning,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaAiPriceOptimizedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI fiyat optimizasyonu alindi: SKU={SKU}, oneri={Price:N2}, rakip_min={CompMin}, guven={Confidence:P0}",
            msg.SKU, msg.RecommendedPrice, msg.CompetitorMinPrice, msg.Confidence);

        _monitor.RecordConsume("ai.price.optimized");
    }
}

public sealed class MesaAiStockPredictedConsumer : IConsumer<MesaAiStockPredictedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaAiStockPredictedConsumer> _logger;

    public MesaAiStockPredictedConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaAiStockPredictedConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaAiStockPredictedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaAiStockPredictedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new UpdateStockForecastCommand
            {
                ProductId = msg.ProductId,
                SKU = msg.SKU,
                PredictedDemand7d = msg.PredictedDemand7d,
                PredictedDemand14d = msg.PredictedDemand14d,
                PredictedDemand30d = msg.PredictedDemand30d,
                DaysUntilStockout = msg.DaysUntilStockout,
                ReorderSuggestion = msg.ReorderSuggestion,
                Confidence = msg.Confidence,
                Reasoning = msg.Reasoning,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaAiStockPredictedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI stok tahmini alindi: SKU={SKU}, 7g={D7}, 14g={D14}, 30g={D30}, tukenis={Days} gun",
            msg.SKU, msg.PredictedDemand7d, msg.PredictedDemand14d, msg.PredictedDemand30d, msg.DaysUntilStockout);

        if (msg.DaysUntilStockout < 7)
        {
            _logger.LogWarning(
                "[MESA Consumer] KRITIK: SKU={SKU} {Days} gun icinde tukenecek! Onerilen siparis: {Reorder} adet",
                msg.SKU, msg.DaysUntilStockout, msg.ReorderSuggestion);
        }

        _monitor.RecordConsume("ai.stock.predicted");
    }
}

public sealed class MesaBotInvoiceRequestConsumer : IConsumer<MesaBotInvoiceRequestedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaBotInvoiceRequestConsumer> _logger;

    public MesaBotInvoiceRequestConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaBotInvoiceRequestConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaBotInvoiceRequestedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaBotInvoiceRequestedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new ProcessBotInvoiceRequestCommand
            {
                CustomerPhone = msg.CustomerPhone,
                OrderNumber = msg.OrderNumber,
                RequestChannel = msg.RequestChannel,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaBotInvoiceRequestedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] Musteri fatura istedi: telefon={Phone}, siparis={Order}, kanal={Channel}",
            MesaConsumerHelpers.MaskPhone(msg.CustomerPhone), msg.OrderNumber, msg.RequestChannel);

        _monitor.RecordConsume("bot.invoice.requested");
    }
}

public sealed class MesaBotReturnRequestConsumer : IConsumer<MesaBotReturnRequestedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MesaBotReturnRequestConsumer> _logger;

    public MesaBotReturnRequestConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<MesaBotReturnRequestConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaBotReturnRequestedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning("[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(MesaBotReturnRequestedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new ProcessBotReturnRequestCommand
            {
                CustomerPhone = msg.CustomerPhone,
                OrderNumber = msg.OrderNumber,
                ReturnReason = msg.ReturnReason,
                RequestChannel = msg.RequestChannel,
                TenantId = tenantId
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(MesaBotReturnRequestedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] Musteri iade istedi: telefon={Phone}, siparis={Order}, sebep={Reason}, kanal={Channel}",
            MesaConsumerHelpers.MaskPhone(msg.CustomerPhone), msg.OrderNumber, msg.ReturnReason, msg.RequestChannel);

        _monitor.RecordConsume("bot.return.requested");
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
