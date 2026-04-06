using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IOrderRepository = MesTech.Domain.Interfaces.IOrderRepository;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// TEST 4/4 — Settlement→MarkAsPaid→GL(102/120).
/// Zincir 4a: SettlementParsed → OrderNumber lookup → Order.MarkAsPaid().
/// Zincir 4b: ReconciliationCompleted → SettlementPaymentGLHandler → JournalEntry(102/120).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "OrderChain")]
public class SettlementMarkAsPaidGLChainTests
{
    private readonly Mock<ISettlementBatchRepository> _settlementRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public SettlementMarkAsPaidGLChainTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ══════════════════════════════════════
    // Z4a: Settlement → Order.MarkAsPaid
    // ══════════════════════════════════════

    [Fact]
    public async Task Z4a_SettlementParsed_OrderMarkedAsPaid()
    {
        var batch = SettlementBatch.Create(TenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1000m, 150m, 810m);
        var line = SettlementLine.Create(TenantId, batch.Id, "ORD-SETTLE-001",
            1000m, 150m, 10m, 30m, 0m, 810m);
        batch.AddLine(line);

        _settlementRepo.Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var order = new Order
        {
            TenantId = TenantId, OrderNumber = "ORD-SETTLE-001",
            CustomerId = Guid.NewGuid(), Status = OrderStatus.Shipped
        };
        _orderRepo.Setup(r => r.GetByOrderNumberAsync("ORD-SETTLE-001", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new SettlementImportedOrderPaymentHandler(
            _settlementRepo.Object, _orderRepo.Object, _uow.Object,
            Mock.Of<ILogger<SettlementImportedOrderPaymentHandler>>());

        var result = await handler.HandleAsync(batch.Id, TenantId, CancellationToken.None);

        result.Matched.Should().Be(1);
        order.PaymentStatus.Should().Be("Paid");
    }

    [Fact]
    public async Task Z4a_SettlementWithCommission_OrderCommissionSet()
    {
        var batch = SettlementBatch.Create(TenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-3), DateTime.UtcNow, 2000m, 300m, 1640m);
        var line = SettlementLine.Create(TenantId, batch.Id, "ORD-COMM-001",
            2000m, 300m, 20m, 40m, 0m, 1640m);
        batch.AddLine(line);

        _settlementRepo.Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var order = new Order
        {
            TenantId = TenantId, OrderNumber = "ORD-COMM-001",
            CustomerId = Guid.NewGuid(), Status = OrderStatus.Delivered
        };
        order.SetFinancials(1694.92m, 305.08m, 2000m);

        _orderRepo.Setup(r => r.GetByOrderNumberAsync("ORD-COMM-001", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new SettlementImportedOrderPaymentHandler(
            _settlementRepo.Object, _orderRepo.Object, _uow.Object,
            Mock.Of<ILogger<SettlementImportedOrderPaymentHandler>>());

        await handler.HandleAsync(batch.Id, TenantId, CancellationToken.None);

        order.PaymentStatus.Should().Be("Paid");
        order.CommissionAmount.Should().Be(300m);
    }

    // ══════════════════════════════════════
    // Z4b: Settlement → GL (102/120)
    // ══════════════════════════════════════

    [Fact]
    public async Task Z4b_PaymentGL_ShouldDebit102Credit120()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Mock settlement batch for amount lookup
        var batch = SettlementBatch.Create(TenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1000m, 150m, 810m);
        _settlementRepo.Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var handler = new SettlementPaymentGLHandler(
            _uow.Object, _journalRepo.Object, _settlementRepo.Object,
            Mock.Of<ILogger<SettlementPaymentGLHandler>>());

        await handler.HandleAsync(
            Guid.NewGuid(), TenantId, batch.Id,
            null, ReconciliationStatus.AutoMatched, CancellationToken.None);

        captured.Should().NotBeNull("GL entry should be created for settlement payment");

        // Debit 102 (Banks) — platform ödeme alındı
        captured!.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account102Banks && l.Debit > 0,
            "DEBIT 102 (Banks) for settlement amount");

        // Credit 120 (Receivables) — alacak kapatıldı
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account120Receivables && l.Credit > 0,
            "CREDIT 120 (Receivables) for settlement amount");
    }

    [Fact]
    public async Task Z4b_PaymentGL_DebitEqualsCredit()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var batch = SettlementBatch.Create(TenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-5), DateTime.UtcNow, 5000m, 750m, 4050m);
        _settlementRepo.Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var handler = new SettlementPaymentGLHandler(
            _uow.Object, _journalRepo.Object, _settlementRepo.Object,
            Mock.Of<ILogger<SettlementPaymentGLHandler>>());

        await handler.HandleAsync(
            Guid.NewGuid(), TenantId, batch.Id,
            null, ReconciliationStatus.AutoMatched, CancellationToken.None);

        if (captured is not null)
        {
            var totalDebit = captured.Lines.Sum(l => l.Debit);
            var totalCredit = captured.Lines.Sum(l => l.Credit);
            totalDebit.Should().Be(totalCredit, "double-entry: debit = credit");
        }
    }

    [Fact]
    public async Task Z4a_AlreadyPaid_ShouldNotRePay()
    {
        var batch = SettlementBatch.Create(TenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow, 500m, 75m, 425m);
        var line = SettlementLine.Create(TenantId, batch.Id, "ORD-REPAY",
            500m, 75m, 0m, 0m, 0m, 425m);
        batch.AddLine(line);

        _settlementRepo.Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var order = new Order
        {
            TenantId = TenantId, OrderNumber = "ORD-REPAY",
            CustomerId = Guid.NewGuid(), Status = OrderStatus.Delivered
        };
        order.MarkAsPaid(); // zaten paid

        _orderRepo.Setup(r => r.GetByOrderNumberAsync("ORD-REPAY", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new SettlementImportedOrderPaymentHandler(
            _settlementRepo.Object, _orderRepo.Object, _uow.Object,
            Mock.Of<ILogger<SettlementImportedOrderPaymentHandler>>());

        var result = await handler.HandleAsync(batch.Id, TenantId, CancellationToken.None);

        result.AlreadyPaid.Should().Be(1);
        result.Matched.Should().Be(0, "already paid order should not be re-matched");
    }
}
