using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Calendar;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Documents;
using MesTech.Domain.Events.Finance;
using MesTech.Domain.Events.Hr;
using MesTech.Domain.Events.Tasks;
using MesTech.Domain.Entities.Finance;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.EventHandlers;

/// <summary>
/// Logger-only event handler testleri — 48 handler x 1 test = 48 test.
/// Her handler: (1) interface uygular, (2) HandleAsync throw etmez.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandlers")]
public class LoggerEventHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ Product Event Handlers ═══

    [Fact]
    public async Task ProductCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ProductCreatedEventHandler(Mock.Of<ILogger<ProductCreatedEventHandler>>());
        var evt = new ProductCreatedEvent(Guid.NewGuid(), _tenantId, "SKU-001", "Test", 99.90m, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IProductCreatedEventHandler>();
    }

    [Fact]
    public async Task ProductUpdatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ProductUpdatedEventHandler(Mock.Of<ILogger<ProductUpdatedEventHandler>>());
        var evt = new ProductUpdatedEvent(Guid.NewGuid(), _tenantId, "SKU-002", DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IProductUpdatedEventHandler>();
    }

    [Fact]
    public async Task PriceChangedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new PriceChangedEventHandler(Mock.Of<ILogger<PriceChangedEventHandler>>());
        var evt = new PriceChangedEvent(Guid.NewGuid(), _tenantId, "SKU-P", 100m, 80m, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IPriceChangedEventHandler>();
    }

    [Fact]
    public async Task StockCriticalEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new StockCriticalEventHandler(Mock.Of<ILogger<StockCriticalEventHandler>>());
        var evt = new StockCriticalEvent(Guid.NewGuid(), _tenantId, "Ürün", "SKU-LOW", 2, 10,
            StockAlertLevel.Critical, null, null, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IStockCriticalEventHandler>();
    }

    // ═══ Order Event Handlers ═══

    [Fact]
    public async Task OrderReceivedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new OrderReceivedEventHandler(Mock.Of<ILogger<OrderReceivedEventHandler>>());
        var evt = new OrderReceivedEvent(Guid.NewGuid(), _tenantId, "Trendyol", "TY-001", 5000m, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IOrderReceivedEventHandler>();
    }

    [Fact]
    public async Task OrderShippedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new OrderShippedEventHandler(Mock.Of<ILogger<OrderShippedEventHandler>>());
        var evt = new OrderShippedEvent(Guid.NewGuid(), _tenantId, "TR123", CargoProvider.YurticiKargo, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IOrderShippedEventHandler>();
    }

    [Fact]
    public async Task OrderCancelledEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new OrderCancelledEventHandler(Mock.Of<ILogger<OrderCancelledEventHandler>>());
        var evt = new OrderCancelledEvent(Guid.NewGuid(), _tenantId, "Trendyol", "TY-C", "Vazgeçti", DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IOrderCancelledEventHandler>();
    }

    // ═══ Expense Event Handlers ═══

    [Fact]
    public async Task ExpenseCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ExpenseCreatedEventHandler(Mock.Of<ILogger<ExpenseCreatedEventHandler>>());
        var evt = new ExpenseCreatedEvent { ExpenseId = Guid.NewGuid(), Title = "Test", Amount = 500m, TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IExpenseCreatedEventHandler>();
    }

    [Fact]
    public async Task ExpensePaidEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ExpensePaidEventHandler(Mock.Of<ILogger<ExpensePaidEventHandler>>());
        var evt = new ExpensePaidEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IExpensePaidEventHandler>();
    }

    [Fact]
    public async Task ExpenseApprovedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ExpenseApprovedEventHandler(Mock.Of<ILogger<ExpenseApprovedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        handler.Should().BeAssignableTo<IExpenseApprovedEventHandler>();
    }

    // ═══ CRM Event Handlers ═══

    [Fact]
    public async Task DealWonEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new DealWonEventHandler(Mock.Of<ILogger<DealWonEventHandler>>());
        var evt = new DealWonEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), 15000m, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IDealWonEventHandler>();
    }

    [Fact]
    public async Task DealLostEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new DealLostEventHandler(Mock.Of<ILogger<DealLostEventHandler>>());
        var evt = new DealLostEvent(Guid.NewGuid(), _tenantId, "Fiyat uyuşmadı", DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IDealLostEventHandler>();
    }

    [Fact]
    public async Task DealStageChangedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new DealStageChangedEventHandler(Mock.Of<ILogger<DealStageChangedEventHandler>>());
        var evt = new DealStageChangedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IDealStageChangedEventHandler>();
    }

    [Fact]
    public async Task LeadConvertedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new LeadConvertedEventHandler(Mock.Of<ILogger<LeadConvertedEventHandler>>());
        var evt = new LeadConvertedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ILeadConvertedEventHandler>();
    }

    // ═══ Invoice Event Handlers ═══

    [Fact]
    public async Task InvoiceApprovedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new InvoiceApprovedEventHandler(Mock.Of<ILogger<InvoiceApprovedEventHandler>>());
        var evt = new InvoiceApprovedEvent(Guid.NewGuid(), _tenantId, "INV-001", 12000m, InvoiceType.EFatura, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IInvoiceApprovedEventHandler>();
    }

    [Fact]
    public async Task InvoiceGeneratedForERPEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new InvoiceGeneratedForERPEventHandler(Mock.Of<ILogger<InvoiceGeneratedForERPEventHandler>>());
        var evt = new InvoiceGeneratedForERPEvent(Guid.NewGuid(), _tenantId, "INV-ERP", 10000m, "Parasut", DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IInvoiceGeneratedForERPEventHandler>();
    }

    [Fact]
    public async Task EInvoiceCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new EInvoiceCreatedEventHandler(Mock.Of<ILogger<EInvoiceCreatedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), "ETTN-001", EInvoiceType.SATIS, CancellationToken.None);
        handler.Should().BeAssignableTo<IEInvoiceCreatedEventHandler>();
    }

    [Fact]
    public async Task EInvoiceSentEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new EInvoiceSentEventHandler(Mock.Of<ILogger<EInvoiceSentEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), "ETTN-002", "REF-123", CancellationToken.None);
        handler.Should().BeAssignableTo<IEInvoiceSentEventHandler>();
    }

    [Fact]
    public async Task EInvoiceCancelledEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new EInvoiceCancelledEventHandler(Mock.Of<ILogger<EInvoiceCancelledEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), "ETTN-003", "Hatalı kesim", CancellationToken.None);
        handler.Should().BeAssignableTo<IEInvoiceCancelledEventHandler>();
    }

    // ═══ Return Event Handlers ═══

    [Fact]
    public async Task ReturnCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ReturnCreatedEventHandler(Mock.Of<ILogger<ReturnCreatedEventHandler>>());
        var evt = new ReturnCreatedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), PlatformType.Trendyol, ReturnReason.DefectiveProduct, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IReturnCreatedEventHandler>();
    }

    [Fact]
    public async Task ReturnResolvedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ReturnResolvedEventHandler(Mock.Of<ILogger<ReturnResolvedEventHandler>>());
        var evt = new ReturnResolvedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), ReturnStatus.Refunded, 500m, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IReturnResolvedEventHandler>();
    }

    // ═══ Finance Event Handlers ═══

    [Fact]
    public async Task BankStatementImportedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new BankStatementImportedEventHandler(Mock.Of<ILogger<BankStatementImportedEventHandler>>());
        var evt = new BankStatementImportedEvent { BankAccountId = Guid.NewGuid(), TransactionCount = 50, TotalInflow = 100000m, TotalOutflow = 45000m, TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IBankStatementImportedEventHandler>();
    }

    [Fact]
    public async Task CashTransactionRecordedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new CashTransactionRecordedEventHandler(Mock.Of<ILogger<CashTransactionRecordedEventHandler>>());
        var evt = new CashTransactionRecordedEvent(_tenantId, Guid.NewGuid(), Guid.NewGuid(), CashTransactionType.Income, 1500m, 5000m, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ICashTransactionRecordedEventHandler>();
    }

    [Fact]
    public async Task ReconciliationCompletedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ReconciliationCompletedEventHandler(Mock.Of<ILogger<ReconciliationCompletedEventHandler>>());
        var evt = new ReconciliationCompletedEvent { MatchId = Guid.NewGuid(), SettlementBatchId = Guid.NewGuid(), BankTransactionId = Guid.NewGuid(), TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IReconciliationCompletedEventHandler>();
    }

    [Fact]
    public async Task ReconciliationMatchedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ReconciliationMatchedEventHandler(Mock.Of<ILogger<ReconciliationMatchedEventHandler>>());
        var evt = new ReconciliationMatchedEvent { ReconciliationMatchId = Guid.NewGuid(), Confidence = 0.95m, TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IReconciliationMatchedEventHandler>();
    }

    [Fact]
    public async Task PaymentFailedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new PaymentFailedEventHandler(Mock.Of<ILogger<PaymentFailedEventHandler>>());
        await handler.HandleAsync(_tenantId, Guid.NewGuid(), "Kart reddedildi", "CARD_DECLINED", 1, CancellationToken.None);
        handler.Should().BeAssignableTo<IPaymentFailedEventHandler>();
    }

    // ═══ Accounting Event Handlers ═══

    [Fact]
    public async Task BaBsRecordCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new BaBsRecordCreatedEventHandler(Mock.Of<ILogger<BaBsRecordCreatedEventHandler>>());
        var evt = new BaBsRecordCreatedEvent { BaBsRecordId = Guid.NewGuid(), Year = 2026, Month = 3, CounterpartyVkn = "1234567890", TotalAmount = 50000m, TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IBaBsRecordCreatedEventHandler>();
    }

    [Fact]
    public async Task FixedAssetCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new FixedAssetCreatedEventHandler(Mock.Of<ILogger<FixedAssetCreatedEventHandler>>());
        var evt = new FixedAssetCreatedEvent { FixedAssetId = Guid.NewGuid(), AssetName = "Bilgisayar", AssetCode = "DM-001", AcquisitionCost = 25000m, TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IFixedAssetCreatedEventHandler>();
    }

    [Fact]
    public async Task TaxWithholdingComputedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new TaxWithholdingComputedEventHandler(Mock.Of<ILogger<TaxWithholdingComputedEventHandler>>());
        var evt = new TaxWithholdingComputedEvent { TaxWithholdingId = Guid.NewGuid(), TaxExclusiveAmount = 10000m, Rate = 0.20m, WithholdingAmount = 2000m, TaxType = "KDV", TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ITaxWithholdingComputedEventHandler>();
    }

    [Fact]
    public async Task ProfitReportGeneratedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new ProfitReportGeneratedEventHandler(Mock.Of<ILogger<ProfitReportGeneratedEventHandler>>());
        var evt = new ProfitReportGeneratedEvent { ReportId = Guid.NewGuid(), Period = "2026-03", NetProfit = 45000m, TenantId = _tenantId };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IProfitReportGeneratedEventHandler>();
    }

    // ═══ Calendar & Task Event Handlers ═══

    [Fact]
    public async Task CalendarEventCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new CalendarEventCreatedEventHandler(Mock.Of<ILogger<CalendarEventCreatedEventHandler>>());
        var evt = new CalendarEventCreatedEvent(Guid.NewGuid(), _tenantId, DateTime.UtcNow, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ICalendarEventCreatedEventHandler>();
    }

    [Fact]
    public async Task TaskCompletedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new TaskCompletedEventHandler(Mock.Of<ILogger<TaskCompletedEventHandler>>());
        var evt = new TaskCompletedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ITaskCompletedEventHandler>();
    }

    [Fact]
    public async Task TaskOverdueEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new TaskOverdueEventHandler(Mock.Of<ILogger<TaskOverdueEventHandler>>());
        var evt = new TaskOverdueEvent(Guid.NewGuid(), _tenantId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ITaskOverdueEventHandler>();
    }

    // ═══ HR Event Handlers ═══

    [Fact]
    public async Task LeaveApprovedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new LeaveApprovedEventHandler(Mock.Of<ILogger<LeaveApprovedEventHandler>>());
        var evt = new LeaveApprovedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ILeaveApprovedEventHandler>();
    }

    [Fact]
    public async Task LeaveRejectedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new LeaveRejectedEventHandler(Mock.Of<ILogger<LeaveRejectedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Yetersiz bakiye", CancellationToken.None);
        handler.Should().BeAssignableTo<ILeaveRejectedEventHandler>();
    }

    // ═══ Document Event Handlers ═══

    [Fact]
    public async Task DocumentUploadedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new DocumentUploadedEventHandler(Mock.Of<ILogger<DocumentUploadedEventHandler>>());
        var evt = new DocumentUploadedEvent(Guid.NewGuid(), "test.pdf", 1024, _tenantId, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IDocumentUploadedEventHandler>();
    }

    // ═══ Sync & Platform Event Handlers ═══

    [Fact]
    public async Task SyncErrorOccurredEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new SyncErrorOccurredEventHandler(Mock.Of<ILogger<SyncErrorOccurredEventHandler>>());
        var evt = new SyncErrorOccurredEvent(_tenantId, "Trendyol", "GetOrders", "Timeout", DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ISyncErrorOccurredEventHandler>();
    }

    [Fact]
    public async Task SyncRequestedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new SyncRequestedEventHandler(Mock.Of<ILogger<SyncRequestedEventHandler>>());
        var evt = new SyncRequestedEvent(_tenantId, "Hepsiburada", SyncDirection.Pull, "Product", null, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ISyncRequestedEventHandler>();
    }

    [Fact]
    public async Task PlatformNotificationFailedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new PlatformNotificationFailedEventHandler(Mock.Of<ILogger<PlatformNotificationFailedEventHandler>>());
        var evt = new PlatformNotificationFailedEvent { TenantId = _tenantId, OrderId = Guid.NewGuid(), PlatformCode = "N11", TrackingNumber = "TR-001", ErrorMessage = "Connection refused", RetryCount = 1 };
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IPlatformNotificationFailedEventHandler>();
    }

    [Fact]
    public async Task PlatformMessageReceivedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new PlatformMessageReceivedEventHandler(Mock.Of<ILogger<PlatformMessageReceivedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), PlatformType.Trendyol, "Müşteri", CancellationToken.None);
        handler.Should().BeAssignableTo<IPlatformMessageReceivedEventHandler>();
    }

    [Fact]
    public async Task BuyboxLostEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new BuyboxLostEventHandler(Mock.Of<ILogger<BuyboxLostEventHandler>>());
        var evt = new BuyboxLostEvent(Guid.NewGuid(), _tenantId, "SKU-BB", 99.90m, 89.90m, "Rakip Satıcı", DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IBuyboxLostEventHandler>();
    }

    [Fact]
    public async Task DailySummaryGeneratedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new DailySummaryGeneratedEventHandler(Mock.Of<ILogger<DailySummaryGeneratedEventHandler>>());
        var evt = new DailySummaryGeneratedEvent(_tenantId, DateTime.UtcNow.Date, 50, 25000m, 3, 12, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<IDailySummaryGeneratedEventHandler>();
    }

    // ═══ Subscription Event Handlers ═══

    [Fact]
    public async Task SubscriptionCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new SubscriptionCreatedEventHandler(Mock.Of<ILogger<SubscriptionCreatedEventHandler>>());
        await handler.HandleAsync(_tenantId, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        handler.Should().BeAssignableTo<ISubscriptionCreatedEventHandler>();
    }

    [Fact]
    public async Task SubscriptionCancelledEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new SubscriptionCancelledEventHandler(Mock.Of<ILogger<SubscriptionCancelledEventHandler>>());
        await handler.HandleAsync(_tenantId, Guid.NewGuid(), "Kullanıcı iptal etti", CancellationToken.None);
        handler.Should().BeAssignableTo<ISubscriptionCancelledEventHandler>();
    }

    // ═══ Notification Event Handlers ═══

    [Fact]
    public async Task NotificationSettingsUpdatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new NotificationSettingsUpdatedEventHandler(Mock.Of<ILogger<NotificationSettingsUpdatedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), NotificationChannel.Email, true, CancellationToken.None);
        handler.Should().BeAssignableTo<INotificationSettingsUpdatedEventHandler>();
    }

    [Fact]
    public async Task OnboardingCompletedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new OnboardingCompletedEventHandler(Mock.Of<ILogger<OnboardingCompletedEventHandler>>());
        await handler.HandleAsync(_tenantId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, CancellationToken.None);
        handler.Should().BeAssignableTo<IOnboardingCompletedEventHandler>();
    }

    // ═══ Price Loss & Stale Order Event Handlers ═══

    [Fact]
    public async Task PriceLossDetectedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new PriceLossDetectedEventHandler(Mock.Of<ILogger<PriceLossDetectedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), "SKU-LOSS", 100m, 80m, 20m, _tenantId, CancellationToken.None);
        handler.Should().BeAssignableTo<IPriceLossEventHandler>();
    }

    [Fact]
    public async Task StaleOrderDetectedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new StaleOrderDetectedEventHandler(Mock.Of<ILogger<StaleOrderDetectedEventHandler>>());
        await handler.HandleAsync(Guid.NewGuid(), "ORD-STALE", PlatformType.Amazon, TimeSpan.FromHours(48), _tenantId, CancellationToken.None);
        handler.Should().BeAssignableTo<IStaleOrderEventHandler>();
    }

    // ═══ Dropshipping Event Handlers ═══

    [Fact]
    public async Task SupplierFeedSyncedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var handler = new SupplierFeedSyncedEventHandler(Mock.Of<ILogger<SupplierFeedSyncedEventHandler>>());
        var evt = new SupplierFeedSyncedEvent(Guid.NewGuid(), _tenantId, Guid.NewGuid(), 150, 3, 0, FeedSyncStatus.Completed, DateTime.UtcNow);
        await handler.HandleAsync(evt, CancellationToken.None);
        handler.Should().BeAssignableTo<ISupplierFeedSyncedEventHandler>();
    }
}
