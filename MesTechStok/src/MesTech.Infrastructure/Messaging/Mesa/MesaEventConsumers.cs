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
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA OS'tan gelen event'leri consume eder.
/// Dalga 1: Sadece log'a yazar.
/// Dalga 2+: Gercek is mantigi eklenir (Product.Description guncelle, fiyat onerisi kaydet vb.)
/// Dalga 5 IP-5: TenantId eklendi — fallback: ITenantProvider.
/// I-13 S-02: 7 consumer deepened — domain logic on top of existing logs.
/// </summary>
public class MesaAiContentConsumer : IConsumer<MesaAiContentGeneratedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaAiContentConsumer> _logger;

    public MesaAiContentConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaAiContentConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

        // Dalga 4: Gercek is mantigi — Product.Description guncelle
        // NOT: DbContext inject edilecek, simdilik log yeterli
        _logger.LogInformation(
            "[MESA Consumer] Product aciklamasi guncellenmeli: SKU={SKU}, uzunluk={Length} karakter",
            msg.SKU, msg.GeneratedContent.Length);

        // I-13 S-02: Product description update
        try
        {
            var product = await _productRepository.GetBySKUAsync(msg.SKU).ConfigureAwait(false);
            if (product is not null)
            {
                product.Description = msg.GeneratedContent;
                product.UpdatedAt = DateTime.UtcNow;
                product.UpdatedBy = "mesa-ai";
                await _productRepository.UpdateAsync(product).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[MESA Consumer] Product description updated: SKU={SKU}", msg.SKU);
            }
            else
            {
                _logger.LogWarning("[MESA Consumer] Product not found for SKU={SKU}, skipping update", msg.SKU);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to update product description: SKU={SKU}", msg.SKU);
            throw; // MassTransit retry
        }
    }
}

public class MesaAiPriceConsumer : IConsumer<MesaAiPriceRecommendedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPriceRecommendationRepository _priceRecommendationRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaAiPriceConsumer> _logger;

    public MesaAiPriceConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IPriceRecommendationRepository priceRecommendationRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaAiPriceConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _priceRecommendationRepository = priceRecommendationRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

        // Dalga 4: PriceRecommendation history + Product snapshot
        // NOT: DbContext inject edilecek, simdilik log yeterli
        _logger.LogInformation(
            "[MESA Consumer] Fiyat onerisi kaydedilmeli: SKU={SKU}, oneri={Price:N2} TL, aralik=[{Min:N2}-{Max:N2}]",
            msg.SKU, msg.RecommendedPrice, msg.MinPrice, msg.MaxPrice);

        // I-13 S-02: Save PriceRecommendation
        try
        {
            var product = await _productRepository.GetBySKUAsync(msg.SKU).ConfigureAwait(false);
            if (product is not null)
            {
                var recommendation = new PriceRecommendation
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    RecommendedPrice = msg.RecommendedPrice,
                    CurrentPrice = product.SalePrice,
                    Confidence = 0, // MesaAiPriceRecommendedEvent does not carry Confidence
                    Reasoning = msg.Reasoning ?? string.Empty,
                    Source = "ai.price.recommended"
                };
                await _priceRecommendationRepository.AddAsync(recommendation).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[MESA Consumer] PriceRecommendation saved: SKU={SKU}, Id={Id}", msg.SKU, recommendation.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to save price recommendation: SKU={SKU}", msg.SKU);
            throw;
        }
    }
}

public class MesaBotStatusConsumer : IConsumer<MesaBotNotificationSentEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaBotStatusConsumer> _logger;

    public MesaBotStatusConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaBotStatusConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
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

        // I-13 S-02: Save NotificationLog
        try
        {
            var notification = NotificationLog.Create(
                tenantId,
                MesTech.Domain.Enums.NotificationChannel.Push,
                msg.Recipient ?? "unknown",
                $"Bot Notification: {msg.Channel}",
                msg.Success ? $"Bildirim başarılı: {msg.Channel}" : $"Bildirim hatası: {msg.ErrorMessage}");
            if (msg.Success) notification.MarkAsSent(); else notification.MarkAsFailed(msg.ErrorMessage ?? "Unknown error");
            await _notificationLogRepository.AddAsync(notification, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to save notification log");
            throw;
        }
    }
}

public class MesaAiPriceOptimizedConsumer : IConsumer<MesaAiPriceOptimizedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPriceRecommendationRepository _priceRecommendationRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaAiPriceOptimizedConsumer> _logger;

    public MesaAiPriceOptimizedConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IPriceRecommendationRepository priceRecommendationRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaAiPriceOptimizedConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _priceRecommendationRepository = priceRecommendationRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

        // Future: PriceRecommendation history INSERT + Product snapshot UPDATE
        // Source = "ai.price.optimized", tek transaction
        // Fiyat farki > %20 ise Telegram Critical alert

        _monitor.RecordConsume("ai.price.optimized");

        // I-13 S-02: Save PriceRecommendation + alert check
        try
        {
            var product = await _productRepository.GetBySKUAsync(msg.SKU).ConfigureAwait(false);
            if (product is not null)
            {
                var recommendation = new PriceRecommendation
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    RecommendedPrice = msg.RecommendedPrice,
                    CurrentPrice = product.SalePrice,
                    CompetitorMinPrice = msg.CompetitorMinPrice,
                    Confidence = msg.Confidence,
                    Reasoning = msg.Reasoning ?? string.Empty,
                    Source = "ai.price.optimized"
                };
                await _priceRecommendationRepository.AddAsync(recommendation).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[MESA Consumer] PriceRecommendation saved: SKU={SKU}, Id={Id}", msg.SKU, recommendation.Id);
            }

            if (product is not null && product.SalePrice > 0)
            {
                var deviationPct = Math.Abs((double)(msg.RecommendedPrice - product.SalePrice) / (double)product.SalePrice);
                if (deviationPct > 0.20)
                {
                    _logger.LogWarning(
                        "[MESA Consumer] PRICE ALERT: SKU={SKU} deviation {Pct:P1} — current={Current:N2}, recommended={Recommended:N2}",
                        msg.SKU, deviationPct, product.SalePrice, msg.RecommendedPrice);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to save price recommendation: SKU={SKU}", msg.SKU);
            throw;
        }
    }
}

public class MesaAiStockPredictedConsumer : IConsumer<MesaAiStockPredictedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IStockPredictionRepository _stockPredictionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaAiStockPredictedConsumer> _logger;

    public MesaAiStockPredictedConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IStockPredictionRepository stockPredictionRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaAiStockPredictedConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _stockPredictionRepository = stockPredictionRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

        // Future: StockPrediction history INSERT + Product snapshot UPDATE (tek transaction)

        _monitor.RecordConsume("ai.stock.predicted");

        // I-13 S-02: Save StockPrediction
        try
        {
            var product = await _productRepository.GetBySKUAsync(msg.SKU).ConfigureAwait(false);
            if (product is not null)
            {
                var prediction = new StockPrediction
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    PredictedDemand7d = msg.PredictedDemand7d,
                    PredictedDemand14d = msg.PredictedDemand14d,
                    PredictedDemand30d = msg.PredictedDemand30d,
                    DaysUntilStockout = msg.DaysUntilStockout,
                    ReorderSuggestion = msg.ReorderSuggestion,
                    Confidence = msg.Confidence,
                    Reasoning = msg.Reasoning ?? string.Empty
                };
                await _stockPredictionRepository.AddAsync(prediction).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[MESA Consumer] StockPrediction saved: SKU={SKU}, Id={Id}", msg.SKU, prediction.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to save stock prediction: SKU={SKU}", msg.SKU);
            throw;
        }
    }
}

public class MesaBotInvoiceRequestConsumer : IConsumer<MesaBotInvoiceRequestedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaBotInvoiceRequestConsumer> _logger;

    public MesaBotInvoiceRequestConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaBotInvoiceRequestConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
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

        // Future: OrderNumber ile Order bul → Invoice bul → PdfUrl
        // Invoice yoksa: "Faturaniz henuz hazirlanmadi" WhatsApp mesaji
        // Invoice varsa: PdfUrl ile "invoice_ready" template gonder

        _monitor.RecordConsume("bot.invoice.requested");

        // I-13 S-02: Order+Invoice lookup
        try
        {
            var order = await _orderRepository.GetByOrderNumberAsync(msg.OrderNumber).ConfigureAwait(false);
            if (order is null)
            {
                _logger.LogWarning("[MESA Consumer] Order not found: OrderNumber={OrderNumber}", msg.OrderNumber);
                return;
            }
            var invoice = await _invoiceRepository.GetByOrderIdAsync(order.Id).ConfigureAwait(false);
            if (invoice is not null)
            {
                _logger.LogInformation(
                    "[MESA Consumer] Invoice found for order: OrderNumber={OrderNumber}, InvoiceId={InvoiceId}, PdfUrl={PdfUrl}",
                    msg.OrderNumber, invoice.Id, invoice.PdfUrl);
                // Future: Send PdfUrl via WhatsApp using IMesaBotService
            }
            else
            {
                _logger.LogInformation(
                    "[MESA Consumer] No invoice yet for order: OrderNumber={OrderNumber}", msg.OrderNumber);
                // Future: Send "faturaniz henuz hazirlanmadi" message
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to lookup invoice: OrderNumber={OrderNumber}", msg.OrderNumber);
            throw;
        }
    }
}

public class MesaBotReturnRequestConsumer : IConsumer<MesaBotReturnRequestedEvent>
{
    private readonly IMediator _mediator;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MesaBotReturnRequestConsumer> _logger;

    public MesaBotReturnRequestConsumer(
        IMediator mediator,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IOrderRepository orderRepository,
        IReturnRequestRepository returnRequestRepository,
        IUnitOfWork unitOfWork,
        ILogger<MesaBotReturnRequestConsumer> logger)
    {
        _mediator = mediator;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _orderRepository = orderRepository;
        _returnRequestRepository = returnRequestRepository;
        _unitOfWork = unitOfWork;
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

        // Future: OrderNumber ile Order bul → ReturnRequest olustur (line'siz)
        // Status: Initiated, Source: WhatsApp
        // Musteriye: "Iade talebiniz alindi #RET-XXX"
        // Saticiya Telegram: "Musteri X iade talep etti, siparis #Y"

        _monitor.RecordConsume("bot.return.requested");

        // I-13 S-02: Create ReturnRequest
        try
        {
            var order = await _orderRepository.GetByOrderNumberAsync(msg.OrderNumber).ConfigureAwait(false);
            if (order is null)
            {
                _logger.LogWarning("[MESA Consumer] Order not found for return: OrderNumber={OrderNumber}", msg.OrderNumber);
                return;
            }
            var returnRequest = new ReturnRequest
            {
                TenantId = tenantId,
                OrderId = order.Id,
                Platform = default, // [Phase-2]: Add PlatformType.Bot or PlatformType.Manual enum value
                CustomerPhone = msg.CustomerPhone,
                ReasonDetail = msg.ReturnReason,
                RequestDate = DateTime.UtcNow,
                Notes = $"Bot return request — channel: {msg.RequestChannel}"
            };
            await _returnRequestRepository.AddAsync(returnRequest).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation(
                "[MESA Consumer] ReturnRequest created: OrderNumber={OrderNumber}, ReturnId={ReturnId}",
                msg.OrderNumber, returnRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] Failed to create return request: OrderNumber={OrderNumber}", msg.OrderNumber);
            throw;
        }
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
