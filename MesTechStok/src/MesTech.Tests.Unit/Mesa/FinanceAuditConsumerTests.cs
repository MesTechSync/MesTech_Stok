using FluentAssertions;
using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// 8 Finance Audit Consumer unit tests.
/// These consumers log + monitor finance outbound events (loopback audit trail).
/// Primary-constructor consumers: (IMesaEventMonitor, ILogger).
/// DEV5: Coverage lift 24% -> 80%+.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
public class FinanceAuditConsumerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> Monitor() => new();
    private static ILogger<T> Logger<T>() => new Mock<ILogger<T>>().Object;

    // ══════════════════════════════════════════════
    //  FinanceAnomalyDetectedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AnomalyDetectedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceAnomalyDetectedAuditConsumer(monitor.Object, Logger<FinanceAnomalyDetectedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceAnomalyDetectedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceAnomalyDetectedEvent(
            "DuplicateInvoice", "Same invoice number detected", 1500m, 3000m,
            "Invoice", Guid.NewGuid(), TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.anomaly.detected"), Times.Once);
    }

    [Fact]
    public async Task AnomalyDetectedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceAnomalyDetectedAuditConsumer(Monitor().Object, Logger<FinanceAnomalyDetectedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceAnomalyDetectedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceAnomalyDetectedEvent(
            "AbnormalExpense", "Large expense detected", null, 50000m,
            null, null, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceBankImportedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BankImportedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceBankImportedAuditConsumer(monitor.Object, Logger<FinanceBankImportedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceBankImportedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceBankImportedEvent(
            Guid.NewGuid(), 150, 25000m, 12000m, DateTime.Today.AddDays(-1),
            TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.bank.imported"), Times.Once);
    }

    [Fact]
    public async Task BankImportedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceBankImportedAuditConsumer(Monitor().Object, Logger<FinanceBankImportedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceBankImportedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceBankImportedEvent(
            Guid.NewGuid(), 0, 0m, 0m, DateTime.Today,
            TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceDocumentReceivedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task DocumentReceivedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceDocumentReceivedAuditConsumer(monitor.Object, Logger<FinanceDocumentReceivedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceDocumentReceivedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceDocumentReceivedEvent(
            Guid.NewGuid(), "fatura-2026-03.pdf", "application/pdf", "WhatsApp",
            TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.document.received"), Times.Once);
    }

    [Fact]
    public async Task DocumentReceivedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceDocumentReceivedAuditConsumer(Monitor().Object, Logger<FinanceDocumentReceivedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceDocumentReceivedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceDocumentReceivedEvent(
            Guid.NewGuid(), "receipt.jpg", "image/jpeg", "Email",
            TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceLedgerPostedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task LedgerPostedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceLedgerPostedAuditConsumer(monitor.Object, Logger<FinanceLedgerPostedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceLedgerPostedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceLedgerPostedEvent(
            Guid.NewGuid(), 5000m, "AutoPost",
            new List<string> { "770", "100" }, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.ledger.posted"), Times.Once);
    }

    [Fact]
    public async Task LedgerPostedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceLedgerPostedAuditConsumer(Monitor().Object, Logger<FinanceLedgerPostedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceLedgerPostedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceLedgerPostedEvent(
            Guid.NewGuid(), 100m, "ManualEntry",
            new List<string> { "600", "320" }, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceReconciliationPendingAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ReconciliationPendingAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceReconciliationPendingAuditConsumer(monitor.Object, Logger<FinanceReconciliationPendingAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceReconciliationPendingEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceReconciliationPendingEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0.85m,
            1200m, 1180m, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.reconciliation.pending"), Times.Once);
    }

    [Fact]
    public async Task ReconciliationPendingAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceReconciliationPendingAuditConsumer(Monitor().Object, Logger<FinanceReconciliationPendingAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceReconciliationPendingEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceReconciliationPendingEvent(
            Guid.NewGuid(), null, null, 0.50m,
            500m, 490m, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceReportDailyAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ReportDailyAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceReportDailyAuditConsumer(monitor.Object, Logger<FinanceReportDailyAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceReportDailyEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceReportDailyEvent(
            DateTime.Today, 42, 25000m, 3000m, 1500m, 20500m,
            new List<string> { "SKU-001 low" },
            new List<string> { "Restock SKU-001" },
            TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.report.daily"), Times.Once);
    }

    [Fact]
    public async Task ReportDailyAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceReportDailyAuditConsumer(Monitor().Object, Logger<FinanceReportDailyAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceReportDailyEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceReportDailyEvent(
            DateTime.Today.AddDays(-1), 0, 0m, 0m, 0m, 0m,
            new List<string>(), new List<string>(),
            TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceSettlementImportedAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SettlementImportedAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceSettlementImportedAuditConsumer(monitor.Object, Logger<FinanceSettlementImportedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceSettlementImportedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceSettlementImportedEvent(
            Guid.NewGuid(), "Trendyol",
            DateTime.Today.AddDays(-14), DateTime.Today,
            18500m, 85, TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.settlement.imported"), Times.Once);
    }

    [Fact]
    public async Task SettlementImportedAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceSettlementImportedAuditConsumer(Monitor().Object, Logger<FinanceSettlementImportedAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceSettlementImportedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceSettlementImportedEvent(
            Guid.NewGuid(), "HepsiBurada",
            DateTime.Today.AddDays(-7), DateTime.Today,
            5000m, 20, TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  FinanceTaxPrepReadyAuditConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task TaxPrepReadyAudit_Consume_RecordsConsume()
    {
        var monitor = Monitor();
        var consumer = new FinanceTaxPrepReadyAuditConsumer(monitor.Object, Logger<FinanceTaxPrepReadyAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceTaxPrepReadyEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceTaxPrepReadyEvent(
            2026, 3, 100000m, 60000m, 18000m, 10800m, 7200m,
            1500m, 500m, "Taslak — resmi beyannameye esas degildir.",
            TestTenantId, DateTime.UtcNow));

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("finance.tax-prep.ready"), Times.Once);
    }

    [Fact]
    public async Task TaxPrepReadyAudit_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new FinanceTaxPrepReadyAuditConsumer(Monitor().Object, Logger<FinanceTaxPrepReadyAuditConsumer>());

        var ctx = new Mock<ConsumeContext<FinanceTaxPrepReadyEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new FinanceTaxPrepReadyEvent(
            2026, 2, 50000m, 30000m, 9000m, 5400m, 3600m,
            800m, 200m, "Draft only.",
            TestTenantId, DateTime.UtcNow));

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }
}
