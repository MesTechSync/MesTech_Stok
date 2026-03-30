using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Mesa;

/// <summary>
/// MESA consumer edge case tests — G473.
/// Tests: Empty TenantId fallback, null message fields, exception propagation.
/// Covers 7 consumers from MesaEventConsumers.cs.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Mesa")]
[Trait("Group", "ConsumerEdgeCase")]
public class MesaConsumerEdgeCaseTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IMesaEventMonitor> _monitor = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Guid _fallbackTenantId = Guid.NewGuid();

    public MesaConsumerEdgeCaseTests()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(_fallbackTenantId);
    }

    private static ConsumeContext<T> MockContext<T>(T message) where T : class
    {
        var ctx = new Mock<ConsumeContext<T>>();
        ctx.Setup(c => c.Message).Returns(message);
        ctx.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        ctx.Setup(c => c.MessageId).Returns(Guid.NewGuid());
        return ctx.Object;
    }

    // ═══════════════════════════════════════
    // MesaAiContentConsumer — Empty TenantId Fallback
    // ═══════════════════════════════════════

    [Fact]
    public async Task AiContent_EmptyTenantId_UsesFallbackFromProvider()
    {
        var consumer = new MesaAiContentConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaAiContentConsumer>>());

        var msg = new MesaAiContentGeneratedEvent(
            ProductId: Guid.NewGuid(),
            SKU: "TEST-SKU",
            GeneratedContent: "AI generated content",
            Metadata: null,
            AiProvider: "claude",
            TenantId: Guid.Empty, // empty — should fallback
            GeneratedAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));

        _mediator.Verify(m => m.Send(
            It.Is<Application.Commands.UpdateProductContent.UpdateProductContentCommand>(c =>
                c.TenantId == _fallbackTenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AiContent_MediatorThrows_ExceptionPropagates()
    {
        _mediator.Setup(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Product not found"));

        var consumer = new MesaAiContentConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaAiContentConsumer>>());

        var msg = new MesaAiContentGeneratedEvent(
            ProductId: Guid.NewGuid(),
            SKU: "SKU-1",
            GeneratedContent: "content",
            Metadata: null,
            AiProvider: "claude",
            TenantId: Guid.NewGuid(),
            GeneratedAt: DateTime.UtcNow);

        var act = async () => await consumer.Consume(MockContext(msg));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ═══════════════════════════════════════
    // MesaAiPriceConsumer — Empty TenantId
    // ═══════════════════════════════════════

    [Fact]
    public async Task AiPrice_EmptyTenantId_UsesFallback()
    {
        var consumer = new MesaAiPriceConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaAiPriceConsumer>>());

        var msg = new MesaAiPriceRecommendedEvent(
            ProductId: Guid.NewGuid(),
            SKU: "PRICE-SKU",
            RecommendedPrice: 99.90m,
            MinPrice: 79.90m,
            MaxPrice: 119.90m,
            Reasoning: "gemini",
            TenantId: Guid.Empty,
            GeneratedAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));

        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════
    // MesaAiPriceOptimizedConsumer
    // ═══════════════════════════════════════

    [Fact]
    public async Task AiPriceOptimized_ValidMessage_SendsCommand()
    {
        var consumer = new MesaAiPriceOptimizedConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaAiPriceOptimizedConsumer>>());

        var msg = new MesaAiPriceOptimizedEvent(
            ProductId: Guid.NewGuid(),
            SKU: "OPT-SKU",
            RecommendedPrice: 149.90m,
            MinPrice: 119.90m,
            MaxPrice: 179.90m,
            CompetitorMinPrice: 129.90m,
            Confidence: 0.85,
            Reasoning: "dynamic-pricing",
            TenantId: Guid.NewGuid(),
            GeneratedAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════
    // MesaAiStockPredictedConsumer
    // ═══════════════════════════════════════

    [Fact]
    public async Task AiStockPredicted_EmptyTenantId_UsesFallback()
    {
        var consumer = new MesaAiStockPredictedConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaAiStockPredictedConsumer>>());

        var msg = new MesaAiStockPredictedEvent(
            ProductId: Guid.NewGuid(),
            SKU: "STOCK-SKU",
            PredictedDemand7d: 150,
            PredictedDemand14d: 300,
            PredictedDemand30d: 600,
            DaysUntilStockout: 7,
            ReorderSuggestion: 200,
            Confidence: 0.80,
            Reasoning: null,
            TenantId: Guid.Empty,
            GeneratedAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════
    // MesaBotStatusConsumer
    // ═══════════════════════════════════════

    [Fact]
    public async Task BotStatus_ValidMessage_SendsCommand()
    {
        var consumer = new MesaBotStatusConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaBotStatusConsumer>>());

        var msg = new MesaBotNotificationSentEvent(
            Channel: "telegram",
            Recipient: "+905551234567",
            Success: true,
            ErrorMessage: null,
            TenantId: Guid.NewGuid(),
            SentAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════
    // MesaBotInvoiceRequestConsumer
    // ═══════════════════════════════════════

    [Fact]
    public async Task BotInvoiceRequest_EmptyTenantId_UsesFallback()
    {
        var consumer = new MesaBotInvoiceRequestConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaBotInvoiceRequestConsumer>>());

        var msg = new MesaBotInvoiceRequestedEvent(
            CustomerPhone: "+905551234567",
            OrderNumber: "ORD-001",
            RequestChannel: "whatsapp",
            TenantId: Guid.Empty,
            RequestedAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════
    // MesaBotReturnRequestConsumer
    // ═══════════════════════════════════════

    [Fact]
    public async Task BotReturnRequest_EmptyTenantId_UsesFallback()
    {
        var consumer = new MesaBotReturnRequestConsumer(
            _mediator.Object, _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<MesaBotReturnRequestConsumer>>());

        var msg = new MesaBotReturnRequestedEvent(
            CustomerPhone: "+905551234567",
            OrderNumber: "ORD-002",
            ReturnReason: "Defective product",
            RequestChannel: "whatsapp",
            TenantId: Guid.Empty,
            RequestedAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
