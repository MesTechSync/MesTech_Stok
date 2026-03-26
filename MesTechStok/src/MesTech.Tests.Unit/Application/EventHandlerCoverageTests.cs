using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Calendar;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Documents;
using MesTech.Domain.Events.Tasks;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Kalan 32 EventHandler testi (logger-only + repo-based)
// ═══════════════════════════════════════════════════════════════

#region Logger-only handlers (notification/audit pattern)

[Trait("Category", "Unit")]
[Trait("Feature", "EventHandler")]
public class LoggerOnlyEventHandlerTests
{
    [Fact]
    public async Task BaBsRecordCreatedEventHandler_Completes()
    {
        var sut = new BaBsRecordCreatedEventHandler(Mock.Of<ILogger<BaBsRecordCreatedEventHandler>>());
        var evt = new BaBsRecordCreatedEvent
        {
            BaBsRecordId = Guid.NewGuid(), Type = BaBsType.Ba,
            CounterpartyVkn = "1234567890", Year = 2026, Month = 3, TotalAmount = 50_000m
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task BankStatementImportedEventHandler_Completes()
    {
        var sut = new BankStatementImportedEventHandler(Mock.Of<ILogger<BankStatementImportedEventHandler>>());
        var evt = new BankStatementImportedEvent
        {
            BankAccountId = Guid.NewGuid(), TransactionCount = 25,
            TotalInflow = 100_000m, TotalOutflow = 45_000m
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task CalendarEventCreatedEventHandler_Completes()
    {
        var sut = new CalendarEventCreatedEventHandler(Mock.Of<ILogger<CalendarEventCreatedEventHandler>>());
        var evt = new CalendarEventCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task CashTransactionRecordedEventHandler_Completes()
    {
        var sut = new CashTransactionRecordedEventHandler(Mock.Of<ILogger<CashTransactionRecordedEventHandler>>());
        var evt = new CashTransactionRecordedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CashTransactionType.Income, 1500m, 25_000m, DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task DealStageChangedEventHandler_Completes()
    {
        var sut = new DealStageChangedEventHandler(Mock.Of<ILogger<DealStageChangedEventHandler>>());
        var evt = new DealStageChangedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task DocumentUploadedEventHandler_Completes()
    {
        var sut = new DocumentUploadedEventHandler(Mock.Of<ILogger<DocumentUploadedEventHandler>>());
        var evt = new DocumentUploadedEvent(Guid.NewGuid(), "fatura.pdf", 1024 * 50, Guid.NewGuid(), DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task EInvoiceCancelledEventHandler_Completes()
    {
        var sut = new EInvoiceCancelledEventHandler(Mock.Of<ILogger<EInvoiceCancelledEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), "ETTN-001", "Müşteri iptali", CancellationToken.None);
    }

    [Fact]
    public async Task EInvoiceCreatedEventHandler_Completes()
    {
        var sut = new EInvoiceCreatedEventHandler(Mock.Of<ILogger<EInvoiceCreatedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), "ETTN-002", EInvoiceType.SATIS, CancellationToken.None);
    }

    [Fact]
    public async Task EInvoiceSentEventHandler_Completes()
    {
        var sut = new EInvoiceSentEventHandler(Mock.Of<ILogger<EInvoiceSentEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), "ETTN-003", "SOVOS-REF-123", CancellationToken.None);
    }

    [Fact]
    public async Task ExpenseApprovedEventHandler_Completes()
    {
        var sut = new ExpenseApprovedEventHandler(Mock.Of<ILogger<ExpenseApprovedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task ExpenseCreatedEventHandler_Completes()
    {
        var sut = new ExpenseCreatedEventHandler(Mock.Of<ILogger<ExpenseCreatedEventHandler>>());
        var evt = new ExpenseCreatedEvent
        {
            ExpenseId = Guid.NewGuid(), Title = "Kira gideri",
            Amount = 5000m, Source = ExpenseSource.Manual
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task FixedAssetCreatedEventHandler_Completes()
    {
        var sut = new FixedAssetCreatedEventHandler(Mock.Of<ILogger<FixedAssetCreatedEventHandler>>());
        var evt = new FixedAssetCreatedEvent
        {
            FixedAssetId = Guid.NewGuid(), AssetName = "Bilgisayar", AssetCode = "DMR-001",
            AcquisitionCost = 25_000m, Method = DepreciationMethod.StraightLine
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task InvoiceGeneratedForERPEventHandler_Completes()
    {
        var sut = new InvoiceGeneratedForERPEventHandler(Mock.Of<ILogger<InvoiceGeneratedForERPEventHandler>>());
        var evt = new InvoiceGeneratedForERPEvent(
            Guid.NewGuid(), Guid.NewGuid(), "INV-100", 11_800m, "Parasut", DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task LeaveRejectedEventHandler_Completes()
    {
        var sut = new LeaveRejectedEventHandler(Mock.Of<ILogger<LeaveRejectedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Yetersiz kota", CancellationToken.None);
    }

    [Fact]
    public async Task NotificationSettingsUpdatedEventHandler_Completes()
    {
        var sut = new NotificationSettingsUpdatedEventHandler(Mock.Of<ILogger<NotificationSettingsUpdatedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), NotificationChannel.Email, true, CancellationToken.None);
    }

    [Fact]
    public async Task OnboardingCompletedEventHandler_Completes()
    {
        var sut = new OnboardingCompletedEventHandler(Mock.Of<ILogger<OnboardingCompletedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, CancellationToken.None);
    }

    [Fact]
    public async Task PaymentFailedEventHandler_Completes()
    {
        var sut = new PaymentFailedEventHandler(Mock.Of<ILogger<PaymentFailedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Card declined", "CARD_DECLINED", 3, CancellationToken.None);
    }

    [Fact]
    public async Task PlatformMessageReceivedEventHandler_Completes()
    {
        var sut = new PlatformMessageReceivedEventHandler(Mock.Of<ILogger<PlatformMessageReceivedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), PlatformType.Trendyol, "Ahmet Yılmaz", CancellationToken.None);
    }

    [Fact]
    public async Task ProductUpdatedEventHandler_Completes()
    {
        var sut = new ProductUpdatedEventHandler(Mock.Of<ILogger<ProductUpdatedEventHandler>>());
        var evt = new ProductUpdatedEvent(Guid.NewGuid(), Guid.NewGuid(), "SKU-001", DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task ProfitReportGeneratedEventHandler_Completes()
    {
        var sut = new ProfitReportGeneratedEventHandler(Mock.Of<ILogger<ProfitReportGeneratedEventHandler>>());
        var evt = new ProfitReportGeneratedEvent
        {
            ReportId = Guid.NewGuid(), Period = "2026-Q1",
            Platform = "Trendyol", NetProfit = 45_000m
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task ReconciliationCompletedEventHandler_Completes()
    {
        var sut = new ReconciliationCompletedEventHandler(Mock.Of<ILogger<ReconciliationCompletedEventHandler>>());
        var evt = new ReconciliationCompletedEvent
        {
            MatchId = Guid.NewGuid(), SettlementBatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            FinalStatus = ReconciliationStatus.AutoMatched, Confidence = 0.95m
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task ReconciliationMatchedEventHandler_Completes()
    {
        var sut = new ReconciliationMatchedEventHandler(Mock.Of<ILogger<ReconciliationMatchedEventHandler>>());
        var evt = new ReconciliationMatchedEvent
        {
            ReconciliationMatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            SettlementBatchId = Guid.NewGuid(), Confidence = 0.88m
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task StaleOrderDetectedEventHandler_Completes()
    {
        var sut = new StaleOrderDetectedEventHandler(Mock.Of<ILogger<StaleOrderDetectedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), "ORD-999", PlatformType.Trendyol, TimeSpan.FromHours(52), Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task SubscriptionCancelledEventHandler_Completes()
    {
        var sut = new SubscriptionCancelledEventHandler(Mock.Of<ILogger<SubscriptionCancelledEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Bütçe kısıtı", CancellationToken.None);
    }

    [Fact]
    public async Task SubscriptionCreatedEventHandler_Completes()
    {
        var sut = new SubscriptionCreatedEventHandler(Mock.Of<ILogger<SubscriptionCreatedEventHandler>>());
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task TaskCompletedEventHandler_Completes()
    {
        var sut = new TaskCompletedEventHandler(Mock.Of<ILogger<TaskCompletedEventHandler>>());
        var evt = new TaskCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task TaskOverdueEventHandler_Completes()
    {
        var sut = new TaskOverdueEventHandler(Mock.Of<ILogger<TaskOverdueEventHandler>>());
        var evt = new TaskOverdueEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task TaxWithholdingComputedEventHandler_Completes()
    {
        var sut = new TaxWithholdingComputedEventHandler(Mock.Of<ILogger<TaxWithholdingComputedEventHandler>>());
        var evt = new TaxWithholdingComputedEvent
        {
            TaxWithholdingId = Guid.NewGuid(), TaxType = "Gelir Vergisi",
            TaxExclusiveAmount = 20_000m, Rate = 0.20m, WithholdingAmount = 4_000m
        };
        await sut.HandleAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task SyncRequestedEventHandler_Completes()
    {
        var sut = new SyncRequestedEventHandler(Mock.Of<ILogger<SyncRequestedEventHandler>>());
        var evt = new SyncRequestedEvent(Guid.NewGuid(), "Trendyol", SyncDirection.Pull, "Stock", null, DateTime.UtcNow);
        await sut.HandleAsync(evt, CancellationToken.None);
    }
}

#endregion

#region OrderShippedCostHandler (repo-based — Zincir 7)

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class OrderShippedCostHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly OrderShippedCostHandler _sut;

    public OrderShippedCostHandlerTests()
    {
        _sut = new OrderShippedCostHandler(
            _uowMock.Object, Mock.Of<ILogger<OrderShippedCostHandler>>());
    }

    [Fact]
    public async Task HandleAsync_PositiveCost_CreatesGLEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TR123456789",
            CargoProvider.YurticiKargo, 45.50m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroCost_SkipsGL()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TR000",
            CargoProvider.ArasKargo, 0m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

// ReturnApprovedHandler birleşik Z5 — tip ayrıştırıldı:
// ReturnApprovedStockRestorationHandler + ReturnJournalReversalHandler
// Testler ChainEventHandlerTests.cs ve ChainIdempotencyTests.cs'de mevcut
