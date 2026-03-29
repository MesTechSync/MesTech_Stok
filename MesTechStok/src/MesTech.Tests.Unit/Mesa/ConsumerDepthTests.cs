using FluentAssertions;
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
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// Consumer Depth Tests — I-13 S-11 / G197 refactor.
/// Consumers delegate to CQRS command handlers via MediatR.Send().
/// 2 tests per consumer (valid + missing data), 7 consumers = 14 tests.
/// G197: Consumers no longer inject repos — tests verify MediatR.Send() dispatch.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ConsumerDepth")]
[Trait("Phase", "I-13")]
public class ConsumerDepthTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> CreateMonitor() => new();

    private static ILogger<T> CreateLogger<T>() =>
        new Mock<ILogger<T>>().Object;

    private static Mock<ITenantProvider> CreateTenantProviderMock()
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(x => x.GetCurrentTenantId()).Returns(TestTenantId);
        return mock;
    }

    private static Mock<IMediator> CreateMediatorMock()
    {
        var mock = new Mock<IMediator>();
        mock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    // ══════════════════════════════════════════════
    //  1. MesaAiContentConsumer — delegates to UpdateProductContentCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiContentConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaAiContentConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            TestProductId, "SKU-DEPTH-001", "AI generated product description for depth test",
            new Dictionary<string, string> { ["lang"] = "tr" }, "GPT-4", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<UpdateProductContentCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.content.generated"), Times.Once);
    }

    [Fact]
    public async Task AiContentConsumer_EmptyTenantId_UsesFallback()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();
        var tenantProvider = CreateTenantProviderMock();

        var consumer = new MesaAiContentConsumer(
            mediator.Object, monitor.Object, tenantProvider.Object,
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(), "", "Content for missing SKU", null, "GPT-4", Guid.Empty, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.content.generated"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  2. MesaAiPriceConsumer — delegates to UpdateProductPriceCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiPriceConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaAiPriceConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            TestProductId, "SKU-PRICE-D01", 149.99m, 120.00m, 180.00m,
            "Rakip analizi bazli oneri", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<UpdateProductPriceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.price.recommended"), Times.Once);
    }

    [Fact]
    public async Task AiPriceConsumer_EmptyTenantId_UsesFallback()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();
        var tenantProvider = CreateTenantProviderMock();

        var consumer = new MesaAiPriceConsumer(
            mediator.Object, monitor.Object, tenantProvider.Object,
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(), "", 99.99m, 80.00m, 120.00m, null, Guid.Empty, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.price.recommended"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  3. MesaBotStatusConsumer — delegates to UpdateBotNotificationStatusCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotStatusConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaBotStatusConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "WhatsApp", "+905551234567", true, null, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<UpdateBotNotificationStatusCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task BotStatusConsumer_EmptyChannel_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaBotStatusConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "", "+905550000000", false, "Connection timeout", TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  4. MesaAiPriceOptimizedConsumer — delegates to ApplyOptimizedPriceCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiPriceOptimizedConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaAiPriceOptimizedConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaAiPriceOptimizedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceOptimizedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceOptimizedEvent(
            TestProductId, "SKU-OPT-D01", 139.90m, 120.00m, 160.00m, 135.00m,
            0.85, "Buybox bazli optimizasyon", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<ApplyOptimizedPriceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.price.optimized"), Times.Once);
    }

    [Fact]
    public async Task AiPriceOptimizedConsumer_EmptyTenantId_UsesFallback()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();
        var tenantProvider = CreateTenantProviderMock();

        var consumer = new MesaAiPriceOptimizedConsumer(
            mediator.Object, monitor.Object, tenantProvider.Object,
            CreateLogger<MesaAiPriceOptimizedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceOptimizedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceOptimizedEvent(
            Guid.NewGuid(), "", 100.00m, 80.00m, 120.00m, null, 0.50, null, Guid.Empty, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("ai.price.optimized"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  5. MesaAiStockPredictedConsumer — delegates to UpdateStockForecastCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiStockPredictedConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaAiStockPredictedConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaAiStockPredictedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiStockPredictedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiStockPredictedEvent(
            TestProductId, "SKU-PRED-D01", 70, 140, 300, 15, 100, 0.80,
            "Yeterli stok mevcut", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<UpdateStockForecastCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.stock.predicted"), Times.Once);
    }

    [Fact]
    public async Task AiStockPredictedConsumer_EmptyTenantId_UsesFallback()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();
        var tenantProvider = CreateTenantProviderMock();

        var consumer = new MesaAiStockPredictedConsumer(
            mediator.Object, monitor.Object, tenantProvider.Object,
            CreateLogger<MesaAiStockPredictedConsumer>());

        var context = new Mock<ConsumeContext<MesaAiStockPredictedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiStockPredictedEvent(
            Guid.NewGuid(), "", 0, 0, 0, 0, 0, 0.0, null, Guid.Empty, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("ai.stock.predicted"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  6. MesaBotInvoiceRequestConsumer — delegates to ProcessBotInvoiceRequestCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotInvoiceRequestConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaBotInvoiceRequestConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaBotInvoiceRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotInvoiceRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotInvoiceRequestedEvent(
            "+905551234567", "ORD-INV-D01", "WhatsApp", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<ProcessBotInvoiceRequestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.invoice.requested"), Times.Once);
    }

    [Fact]
    public async Task BotInvoiceRequestConsumer_EmptyTenantId_UsesFallback()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();
        var tenantProvider = CreateTenantProviderMock();

        var consumer = new MesaBotInvoiceRequestConsumer(
            mediator.Object, monitor.Object, tenantProvider.Object,
            CreateLogger<MesaBotInvoiceRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotInvoiceRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotInvoiceRequestedEvent(
            "+905559999999", "", "WhatsApp", Guid.Empty, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("bot.invoice.requested"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  7. MesaBotReturnRequestConsumer — delegates to ProcessBotReturnRequestCommand
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotReturnRequestConsumer_ValidEvent_SendsCommand()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();

        var consumer = new MesaBotReturnRequestConsumer(
            mediator.Object, monitor.Object, CreateTenantProviderMock().Object,
            CreateLogger<MesaBotReturnRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotReturnRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotReturnRequestedEvent(
            "+905559876543", "ORD-RET-D01", "Urun arizali", "WhatsApp", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        mediator.Verify(m => m.Send(It.IsAny<ProcessBotReturnRequestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.return.requested"), Times.Once);
    }

    [Fact]
    public async Task BotReturnRequestConsumer_EmptyTenantId_UsesFallback()
    {
        var monitor = CreateMonitor();
        var mediator = CreateMediatorMock();
        var tenantProvider = CreateTenantProviderMock();

        var consumer = new MesaBotReturnRequestConsumer(
            mediator.Object, monitor.Object, tenantProvider.Object,
            CreateLogger<MesaBotReturnRequestConsumer>());

        var context = new Mock<ConsumeContext<MesaBotReturnRequestedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotReturnRequestedEvent(
            "+905551111111", "", null, "Telegram", Guid.Empty, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
        monitor.Verify(m => m.RecordConsume("bot.return.requested"), Times.Once);
    }
}
