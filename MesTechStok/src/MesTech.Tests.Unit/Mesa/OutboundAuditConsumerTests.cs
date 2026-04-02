using FluentAssertions;
using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Messaging.Mesa.Consumers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// 12 Outbound Audit Consumer unit tests.
/// These consumers log + monitor MESA outbound events (loopback audit trail).
/// Primary-constructor consumers: (IMesaEventMonitor, ILogger).
/// DEV5: Coverage lift 24% -> 80%+.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
public class OutboundAuditConsumerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> Monitor() => new();
    private static ILogger<T> Logger<T>() => new Mock<ILogger<T>>().Object;

    // ══════════════════════════════════════════════
    //  MesaProductCreatedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ProductCreatedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaProductCreatedAuditConsumer(monitor.Object, Logger<MesaProductCreatedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaProductCreatedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaProductCreatedEvent(
            Guid.NewGuid(), "SKU-001", "Test Product", "Electronics", 199.99m,
            null, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.product.created"), Times.Once);
    }

    [Fact]
    public async Task ProductCreatedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaProductCreatedAuditConsumer(Monitor().Object, Logger<MesaProductCreatedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaProductCreatedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaProductCreatedEvent(
            Guid.NewGuid(), "SKU-002", "Product 2", null, 50m,
            new List<string> { "https://img.test/1.jpg" }, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaStockLowAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task StockLowAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaStockLowAuditConsumer(monitor.Object, Logger<MesaStockLowAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaStockLowEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaStockLowEvent(
            Guid.NewGuid(), "SKU-LOW-001", 3, 10, 50, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.stock.low"), Times.Once);
    }

    [Fact]
    public async Task StockLowAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaStockLowAuditConsumer(Monitor().Object, Logger<MesaStockLowAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaStockLowEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaStockLowEvent(
            Guid.NewGuid(), "SKU-LOW-002", 0, 5, null, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaOrderReceivedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task OrderReceivedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaOrderReceivedAuditConsumer(monitor.Object, Logger<MesaOrderReceivedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaOrderReceivedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaOrderReceivedEvent(
            Guid.NewGuid(), "Trendyol", "TY-123456", 599.90m, "+905551112233",
            TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.order.received"), Times.Once);
    }

    [Fact]
    public async Task OrderReceivedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaOrderReceivedAuditConsumer(Monitor().Object, Logger<MesaOrderReceivedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaOrderReceivedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaOrderReceivedEvent(
            Guid.NewGuid(), "HepsiBurada", "HB-789", 150m, null,
            TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaPriceChangedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PriceChangedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaPriceChangedAuditConsumer(monitor.Object, Logger<MesaPriceChangedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaPriceChangedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaPriceChangedEvent(
            Guid.NewGuid(), "SKU-PRC-001", 100m, 89.90m, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.price.changed"), Times.Once);
    }

    [Fact]
    public async Task PriceChangedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaPriceChangedAuditConsumer(Monitor().Object, Logger<MesaPriceChangedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaPriceChangedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaPriceChangedEvent(
            Guid.NewGuid(), "SKU-PRC-002", 250m, 300m, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaInvoiceGeneratedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task InvoiceGeneratedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaInvoiceGeneratedAuditConsumer(monitor.Object, Logger<MesaInvoiceGeneratedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaInvoiceGeneratedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaInvoiceGeneratedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "INV-2026-001", "EFatura",
            "Test Customer", "test@test.com", "+905551234567", 1500m,
            "https://storage/inv.pdf", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.invoice.generated"), Times.Once);
    }

    [Fact]
    public async Task InvoiceGeneratedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaInvoiceGeneratedAuditConsumer(Monitor().Object, Logger<MesaInvoiceGeneratedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaInvoiceGeneratedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaInvoiceGeneratedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "INV-2026-002", "EArsiv",
            null, null, null, 250m, null, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaInvoiceCancelledAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task InvoiceCancelledAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaInvoiceCancelledAuditConsumer(monitor.Object, Logger<MesaInvoiceCancelledAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaInvoiceCancelledEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaInvoiceCancelledEvent(
            Guid.NewGuid(), "INV-CANCEL-001", "Musteri talebi", TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.invoice.cancelled"), Times.Once);
    }

    [Fact]
    public async Task InvoiceCancelledAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaInvoiceCancelledAuditConsumer(Monitor().Object, Logger<MesaInvoiceCancelledAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaInvoiceCancelledEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaInvoiceCancelledEvent(
            Guid.NewGuid(), "INV-CANCEL-002", null, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaReturnCreatedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ReturnCreatedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaReturnCreatedAuditConsumer(monitor.Object, Logger<MesaReturnCreatedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaReturnCreatedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaReturnCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Trendyol", "Ahmet Yilmaz",
            "+905559991122", "Urun arizali", 2, 340m, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.return.created"), Times.Once);
    }

    [Fact]
    public async Task ReturnCreatedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaReturnCreatedAuditConsumer(Monitor().Object, Logger<MesaReturnCreatedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaReturnCreatedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaReturnCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "N11", null, null, "Yanlislik",
            1, 99.90m, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaReturnResolvedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ReturnResolvedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaReturnResolvedAuditConsumer(monitor.Object, Logger<MesaReturnResolvedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaReturnResolvedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaReturnResolvedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Refunded", 340m, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.return.resolved"), Times.Once);
    }

    [Fact]
    public async Task ReturnResolvedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaReturnResolvedAuditConsumer(Monitor().Object, Logger<MesaReturnResolvedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaReturnResolvedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaReturnResolvedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Rejected", 0m, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaBuyboxLostAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BuyboxLostAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaBuyboxLostAuditConsumer(monitor.Object, Logger<MesaBuyboxLostAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaBuyboxLostEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaBuyboxLostEvent(
            Guid.NewGuid(), "SKU-BB-001", 150m, 139.90m, "CompetitorX",
            10.10m, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.buybox.lost"), Times.Once);
    }

    [Fact]
    public async Task BuyboxLostAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaBuyboxLostAuditConsumer(Monitor().Object, Logger<MesaBuyboxLostAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaBuyboxLostEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaBuyboxLostEvent(
            Guid.NewGuid(), "SKU-BB-002", 200m, 185m, "CompetitorY",
            15m, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaSupplierFeedSyncedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SupplierFeedSyncedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaSupplierFeedSyncedAuditConsumer(monitor.Object, Logger<MesaSupplierFeedSyncedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaSupplierFeedSyncedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaSupplierFeedSyncedEvent(
            Guid.NewGuid(), "AcmeSupplier", "XML", 500, 20, 30, 5,
            TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.supplier.feed.synced"), Times.Once);
    }

    [Fact]
    public async Task SupplierFeedSyncedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaSupplierFeedSyncedAuditConsumer(Monitor().Object, Logger<MesaSupplierFeedSyncedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaSupplierFeedSyncedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaSupplierFeedSyncedEvent(
            Guid.NewGuid(), "BetaSupplier", "CSV", 1000, 0, 100, 10,
            TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaDailySummaryAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task DailySummaryAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaDailySummaryAuditConsumer(monitor.Object, Logger<MesaDailySummaryAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaDailySummaryEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaDailySummaryEvent(
            TestTenantId, DateTime.Today, 42, 15890.50m, 3, 38, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.daily.summary"), Times.Once);
    }

    [Fact]
    public async Task DailySummaryAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaDailySummaryAuditConsumer(Monitor().Object, Logger<MesaDailySummaryAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaDailySummaryEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaDailySummaryEvent(
            TestTenantId, DateTime.Today.AddDays(-1), 0, 0m, 0, 0, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  MesaSyncErrorAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SyncErrorAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new MesaSyncErrorAuditConsumer(monitor.Object, Logger<MesaSyncErrorAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaSyncErrorEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaSyncErrorEvent(
            TestTenantId, "Trendyol", "ApiTimeout", "Request timed out after 30s",
            DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("mesa.sync.error"), Times.Once);
    }

    [Fact]
    public async Task SyncErrorAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new MesaSyncErrorAuditConsumer(Monitor().Object, Logger<MesaSyncErrorAuditConsumer>());

        var ctx = new Mock<ConsumeContext<MesaSyncErrorEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new MesaSyncErrorEvent(
            TestTenantId, "HepsiBurada", "AuthError", "Token expired",
            DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }
}
