using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MESA OS consumer unit testleri — DEV 5 Dalga 3 Hafta 12.
/// Mock ConsumeContext ile MassTransit pipeline dogrulama.
/// RabbitMQ Docker bagimli degil — tamamen unit test.
/// Dalga 5 IP-5: ITenantProvider eklendi.
/// G197: Constructor signatures simplified — repo/UoW deps removed, logic in CQRS handlers.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
[Trait("Phase", "Dalga3")]
public class MesaConsumerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> CreateMonitor() => new();

    private static ILogger<T> CreateLogger<T>() =>
        new Mock<ILogger<T>>().Object;

    private static ITenantProvider CreateTenantProvider()
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(x => x.GetCurrentTenantId()).Returns(TestTenantId);
        return mock.Object;
    }

    private static IMediator StubMediator() => new Mock<IMediator>().Object;

    // ══════════════════════════════════════════════
    //  MesaAiContentConsumer (4 tests)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiContent_Consume_CallsMonitorRecordConsume()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiContentConsumer(
            StubMediator(), monitorMock.Object, CreateTenantProvider(),
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(), "SKU-AI-001", "Generated content text",
            null, "GPT-4", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("ai.content.generated"), Times.Once);
    }

    [Fact]
    public async Task AiContent_Consume_WithMetadata_DoesNotThrow()
    {
        var consumer = new MesaAiContentConsumer(
            StubMediator(), CreateMonitor().Object, CreateTenantProvider(),
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(), "SKU-AI-002", "Generated content with metadata",
            new Dictionary<string, string> { ["lang"] = "tr", ["tone"] = "formal" },
            "GPT-4", TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiContent_Consume_NullMetadata_DoesNotThrow()
    {
        var consumer = new MesaAiContentConsumer(
            StubMediator(), CreateMonitor().Object, CreateTenantProvider(),
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(), "SKU-AI-003", "Generated content no metadata",
            null, "Claude", TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiContent_Consume_SetsConsumeKey()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiContentConsumer(
            StubMediator(), monitorMock.Object, CreateTenantProvider(),
            CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(), "SKU-AI-004", "Content for key verification",
            null, "GPT-4", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(
            m => m.RecordConsume(It.Is<string>(s => s == "ai.content.generated")),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  MesaAiPriceConsumer (4 tests)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiPrice_Consume_CallsMonitorRecordConsume()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiPriceConsumer(
            StubMediator(), monitorMock.Object, CreateTenantProvider(),
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(), "SKU-PRICE-001", 149.99m, 120.00m, 180.00m,
            null, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("ai.price.recommended"), Times.Once);
    }

    [Fact]
    public async Task AiPrice_Consume_WithReasoning_DoesNotThrow()
    {
        var consumer = new MesaAiPriceConsumer(
            StubMediator(), CreateMonitor().Object, CreateTenantProvider(),
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(), "SKU-PRICE-002", 299.90m, 250.00m, 350.00m,
            "Rakip fiyatlari ve mevsimsel trend analizi", TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiPrice_Consume_NullReasoning_DoesNotThrow()
    {
        var consumer = new MesaAiPriceConsumer(
            StubMediator(), CreateMonitor().Object, CreateTenantProvider(),
            CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(), "SKU-PRICE-003", 89.99m, 70.00m, 110.00m,
            null, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiPrice_Consume_PriceFieldsAvailableInMessage()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiPriceConsumer(
            StubMediator(), monitorMock.Object, CreateTenantProvider(),
            CreateLogger<MesaAiPriceConsumer>());

        var expectedRecommended = 199.99m;
        var expectedMin = 150.00m;
        var expectedMax = 250.00m;

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(), "SKU-PRICE-004", expectedRecommended, expectedMin, expectedMax,
            null, TestTenantId, DateTime.UtcNow));

        var capturedMessage = context.Object.Message;
        await consumer.Consume(context.Object);

        capturedMessage.RecommendedPrice.Should().Be(expectedRecommended);
        capturedMessage.MinPrice.Should().Be(expectedMin);
        capturedMessage.MaxPrice.Should().Be(expectedMax);
    }

    // ══════════════════════════════════════════════
    //  MesaBotStatusConsumer (4 tests)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotStatus_Success_CallsMonitorRecordConsume()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaBotStatusConsumer(
            StubMediator(), monitorMock.Object, CreateTenantProvider(),
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "WhatsApp", "+905551234567", true, null, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task BotStatus_Failure_CallsMonitorRecordConsume()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaBotStatusConsumer(
            StubMediator(), monitorMock.Object, CreateTenantProvider(),
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "Telegram", "@testuser", false, "Connection timeout", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task BotStatus_Success_DoesNotThrow()
    {
        var consumer = new MesaBotStatusConsumer(
            StubMediator(), CreateMonitor().Object, CreateTenantProvider(),
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "WhatsApp", "+905559876543", true, null, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BotStatus_Failure_WithErrorMessage_DoesNotThrow()
    {
        var consumer = new MesaBotStatusConsumer(
            StubMediator(), CreateMonitor().Object, CreateTenantProvider(),
            CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "Telegram", "@erroruser", false, "Some error", TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaAiPriceOptimizedConsumer (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiPriceOptimized_ShouldRecordConsume()
    {
        var monitor = CreateMonitor();
        var consumer = new MesaAiPriceOptimizedConsumer(
            StubMediator(), monitor.Object, CreateTenantProvider(),
            CreateLogger<MesaAiPriceOptimizedConsumer>());
        var mockContext = new Mock<ConsumeContext<MesaAiPriceOptimizedEvent>>();
        mockContext.Setup(x => x.Message).Returns(new MesaAiPriceOptimizedEvent(
            Guid.NewGuid(), "SKU-OPT-001", 139.90m, 120m, 160m, 135m, 0.85, "Buybox bazli", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(mockContext.Object);

        monitor.Verify(x => x.RecordConsume("ai.price.optimized"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  MesaAiStockPredictedConsumer (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiStockPredicted_ShouldRecordConsume()
    {
        var monitor = CreateMonitor();
        var consumer = new MesaAiStockPredictedConsumer(
            StubMediator(), monitor.Object, CreateTenantProvider(),
            CreateLogger<MesaAiStockPredictedConsumer>());
        var mockContext = new Mock<ConsumeContext<MesaAiStockPredictedEvent>>();
        mockContext.Setup(x => x.Message).Returns(new MesaAiStockPredictedEvent(
            Guid.NewGuid(), "SKU-PRED-001", 70, 140, 300, 15, 100, 0.80, "Yeterli stok", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(mockContext.Object);

        monitor.Verify(x => x.RecordConsume("ai.stock.predicted"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  MesaBotInvoiceRequestConsumer (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotInvoiceRequest_ShouldRecordConsume()
    {
        var monitor = CreateMonitor();
        var consumer = new MesaBotInvoiceRequestConsumer(
            StubMediator(), monitor.Object, CreateTenantProvider(),
            CreateLogger<MesaBotInvoiceRequestConsumer>());
        var mockContext = new Mock<ConsumeContext<MesaBotInvoiceRequestedEvent>>();
        mockContext.Setup(x => x.Message).Returns(new MesaBotInvoiceRequestedEvent(
            "+905551234567", "ORD-2026-001", "WhatsApp", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(mockContext.Object);

        monitor.Verify(x => x.RecordConsume("bot.invoice.requested"), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  MesaBotReturnRequestConsumer (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotReturnRequest_ShouldRecordConsume()
    {
        var monitor = CreateMonitor();
        var consumer = new MesaBotReturnRequestConsumer(
            StubMediator(), monitor.Object, CreateTenantProvider(),
            CreateLogger<MesaBotReturnRequestConsumer>());
        var mockContext = new Mock<ConsumeContext<MesaBotReturnRequestedEvent>>();
        mockContext.Setup(x => x.Message).Returns(new MesaBotReturnRequestedEvent(
            "+905559876543", "ORD-2026-002", "Urun arizali", "WhatsApp", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(mockContext.Object);

        monitor.Verify(x => x.RecordConsume("bot.return.requested"), Times.Once);
    }
}
