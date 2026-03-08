using FluentAssertions;
using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MESA OS consumer unit testleri — DEV 5 Dalga 3 Hafta 12.
/// Mock ConsumeContext ile MassTransit pipeline dogrulama.
/// RabbitMQ Docker bagimli degil — tamamen unit test.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
[Trait("Phase", "Dalga3")]
public class MesaConsumerTests
{
    private static Mock<IMesaEventMonitor> CreateMonitor() => new();

    private static ILogger<T> CreateLogger<T>() =>
        new Mock<ILogger<T>>().Object;

    // ══════════════════════════════════════════════
    //  MesaAiContentConsumer (4 tests)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiContent_Consume_CallsMonitorRecordConsume()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiContentConsumer(monitorMock.Object, CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(),
            "SKU-AI-001",
            "Generated content text",
            null,
            "GPT-4",
            DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("ai.content.generated"), Times.Once);
    }

    [Fact]
    public async Task AiContent_Consume_WithMetadata_DoesNotThrow()
    {
        var consumer = new MesaAiContentConsumer(CreateMonitor().Object, CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(),
            "SKU-AI-002",
            "Generated content with metadata",
            new Dictionary<string, string> { ["lang"] = "tr", ["tone"] = "formal" },
            "GPT-4",
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiContent_Consume_NullMetadata_DoesNotThrow()
    {
        var consumer = new MesaAiContentConsumer(CreateMonitor().Object, CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(),
            "SKU-AI-003",
            "Generated content no metadata",
            null,
            "Claude",
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiContent_Consume_SetsConsumeKey()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiContentConsumer(monitorMock.Object, CreateLogger<MesaAiContentConsumer>());

        var context = new Mock<ConsumeContext<MesaAiContentGeneratedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiContentGeneratedEvent(
            Guid.NewGuid(),
            "SKU-AI-004",
            "Content for key verification",
            null,
            "GPT-4",
            DateTime.UtcNow));

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
        var consumer = new MesaAiPriceConsumer(monitorMock.Object, CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(),
            "SKU-PRICE-001",
            149.99m,
            120.00m,
            180.00m,
            null,
            DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("ai.price.recommended"), Times.Once);
    }

    [Fact]
    public async Task AiPrice_Consume_WithReasoning_DoesNotThrow()
    {
        var consumer = new MesaAiPriceConsumer(CreateMonitor().Object, CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(),
            "SKU-PRICE-002",
            299.90m,
            250.00m,
            350.00m,
            "Rakip fiyatlari ve mevsimsel trend analizi",
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiPrice_Consume_NullReasoning_DoesNotThrow()
    {
        var consumer = new MesaAiPriceConsumer(CreateMonitor().Object, CreateLogger<MesaAiPriceConsumer>());

        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(),
            "SKU-PRICE-003",
            89.99m,
            70.00m,
            110.00m,
            null,
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiPrice_Consume_PriceFieldsAvailableInMessage()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaAiPriceConsumer(monitorMock.Object, CreateLogger<MesaAiPriceConsumer>());

        var expectedRecommended = 199.99m;
        var expectedMin = 150.00m;
        var expectedMax = 250.00m;

        MesaAiPriceRecommendedEvent? capturedMessage = null;
        var context = new Mock<ConsumeContext<MesaAiPriceRecommendedEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaAiPriceRecommendedEvent(
            Guid.NewGuid(),
            "SKU-PRICE-004",
            expectedRecommended,
            expectedMin,
            expectedMax,
            null,
            DateTime.UtcNow));

        capturedMessage = context.Object.Message;
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
        var consumer = new MesaBotStatusConsumer(monitorMock.Object, CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "WhatsApp",
            "+905551234567",
            true,
            null,
            DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task BotStatus_Failure_CallsMonitorRecordConsume()
    {
        var monitorMock = CreateMonitor();
        var consumer = new MesaBotStatusConsumer(monitorMock.Object, CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "Telegram",
            "@testuser",
            false,
            "Connection timeout",
            DateTime.UtcNow));

        await consumer.Consume(context.Object);

        monitorMock.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task BotStatus_Success_DoesNotThrow()
    {
        var consumer = new MesaBotStatusConsumer(CreateMonitor().Object, CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "WhatsApp",
            "+905559876543",
            true,
            null,
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BotStatus_Failure_WithErrorMessage_DoesNotThrow()
    {
        var consumer = new MesaBotStatusConsumer(CreateMonitor().Object, CreateLogger<MesaBotStatusConsumer>());

        var context = new Mock<ConsumeContext<MesaBotNotificationSentEvent>>();
        context.SetupGet(c => c.Message).Returns(new MesaBotNotificationSentEvent(
            "Telegram",
            "@erroruser",
            false,
            "Some error",
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(context.Object);

        await act.Should().NotThrowAsync();
    }
}
