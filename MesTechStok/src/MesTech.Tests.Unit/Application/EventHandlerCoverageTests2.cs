using MesTech.Application.EventHandlers;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Finance;
using MesTech.Domain.Events.Hr;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class EventHandlerCoverageTests2
{
    private static readonly Guid _t = Guid.NewGuid();

    [Fact]
    public async Task DealWonEventHandler_Completes() =>
        await new DealWonEventHandler(Mock.Of<ILogger<DealWonEventHandler>>()))
            .HandleAsync(new DealWonEvent(Guid.NewGuid(), _t, Guid.NewGuid(), 1500m, DateTime.UtcNow), default);

    [Fact]
    public async Task DealLostEventHandler_Completes() =>
        await new DealLostEventHandler(Mock.Of<ILogger<DealLostEventHandler>>()))
            .HandleAsync(new DealLostEvent(Guid.NewGuid(), _t, "Price", DateTime.UtcNow), default);

    [Fact]
    public async Task BuyboxLostEventHandler_Completes() =>
        await new BuyboxLostEventHandler(Mock.Of<ILogger<BuyboxLostEventHandler>>())
            .HandleAsync(new BuyboxLostEvent(Guid.NewGuid(), _t, "SKU-001", 100m, 90m, "Trendyol", DateTime.UtcNow), default);

    [Fact]
    public async Task DailySummaryGeneratedEventHandler_Completes() =>
        await new DailySummaryGeneratedEventHandler(Mock.Of<ILogger<DailySummaryGeneratedEventHandler>>())
            .HandleAsync(new DailySummaryGeneratedEvent(_t, DateTime.Today, 10, 5000m, 2, 3, DateTime.UtcNow), default);

    [Fact]
    public async Task ExpensePaidEventHandler_Completes() =>
        await new ExpensePaidEventHandler(Mock.Of<ILogger<ExpensePaidEventHandler>>())
            .HandleAsync(new ExpensePaidEvent(Guid.NewGuid(), _t, Guid.NewGuid(), DateTime.UtcNow), default);

    [Fact]
    public async Task LeadConvertedEventHandler_Completes() =>
        await new LeadConvertedEventHandler(Mock.Of<ILogger<LeadConvertedEventHandler>>())
            .HandleAsync(new LeadConvertedEvent(Guid.NewGuid(), _t, Guid.NewGuid(), DateTime.UtcNow), default);

    [Fact]
    public async Task LeaveApprovedEventHandler_Completes() =>
        await new LeaveApprovedEventHandler(Mock.Of<ILogger<LeaveApprovedEventHandler>>())
            .HandleAsync(new LeaveApprovedEvent(Guid.NewGuid(), _t, Guid.NewGuid(), DateTime.UtcNow), default);

    [Fact]
    public async Task OrderCancelledEventHandler_Completes() =>
        await new OrderCancelledEventHandler(Mock.Of<ILogger<OrderCancelledEventHandler>>())
            .HandleAsync(new OrderCancelledEvent(Guid.NewGuid(), _t, "Trendyol", "TR-123", "Cancel", DateTime.UtcNow), default);

    [Fact]
    public async Task OrderReceivedEventHandler_Completes() =>
        await new OrderReceivedEventHandler(Mock.Of<ILogger<OrderReceivedEventHandler>>())
            .HandleAsync(new OrderReceivedEvent(Guid.NewGuid(), _t, "Trendyol", "TR-456", 250m, DateTime.UtcNow), default);

    [Fact]
    public async Task OrderShippedEventHandler_Completes() =>
        await new OrderShippedEventHandler(Mock.Of<ILogger<OrderShippedEventHandler>>())
            .HandleAsync(new OrderShippedEvent(Guid.NewGuid(), _t, "TR12345", CargoProvider.YurticiKargo, DateTime.UtcNow), default);

    [Fact]
    public async Task PriceChangedEventHandler_Completes() =>
        await new PriceChangedEventHandler(Mock.Of<ILogger<PriceChangedEventHandler>>())
            .HandleAsync(new PriceChangedEvent(Guid.NewGuid(), _t, "SKU-002", 100m, 120m, DateTime.UtcNow), default);

    [Fact]
    public async Task ProductCreatedEventHandler_Completes() =>
        await new ProductCreatedEventHandler(Mock.Of<ILogger<ProductCreatedEventHandler>>())
            .HandleAsync(new ProductCreatedEvent(Guid.NewGuid(), _t, "NEW-001", "Test", 99.90m, DateTime.UtcNow), default);

    [Fact]
    public async Task ReturnCreatedEventHandler_Completes() =>
        await new ReturnCreatedEventHandler(Mock.Of<ILogger<ReturnCreatedEventHandler>>())
            .HandleAsync(new ReturnCreatedEvent(Guid.NewGuid(), _t, Guid.NewGuid(), PlatformType.Trendyol, ReturnReason.DefectiveProduct, DateTime.UtcNow), default);

    [Fact]
    public async Task ReturnResolvedEventHandler_Completes() =>
        await new ReturnResolvedEventHandler(Mock.Of<ILogger<ReturnResolvedEventHandler>>())
            .HandleAsync(new ReturnResolvedEvent(Guid.NewGuid(), _t, Guid.NewGuid(), ReturnStatus.Approved, 150m, DateTime.UtcNow), default);

    [Fact]
    public async Task StockCriticalEventHandler_Completes() =>
        await new StockCriticalEventHandler(Mock.Of<ILogger<StockCriticalEventHandler>>())
            .HandleAsync(new StockCriticalEvent(Guid.NewGuid(), _t, "Test", "SKU-003", 2, 5, StockAlertLevel.Critical, null, "Ana Depo", DateTime.UtcNow), default);

    [Fact]
    public async Task SupplierFeedSyncedEventHandler_Completes() =>
        await new SupplierFeedSyncedEventHandler(Mock.Of<ILogger<SupplierFeedSyncedEventHandler>>())
            .HandleAsync(new SupplierFeedSyncedEvent(Guid.NewGuid(), _t, Guid.NewGuid(), 100, 10, 2, FeedSyncStatus.Completed, DateTime.UtcNow), default);

    [Fact]
    public async Task SyncErrorOccurredEventHandler_Completes() =>
        await new SyncErrorOccurredEventHandler(Mock.Of<ILogger<SyncErrorOccurredEventHandler>>())
            .HandleAsync(new SyncErrorOccurredEvent(_t, "Trendyol", "ApiTimeout", "Timeout", DateTime.UtcNow), default);

    [Fact]
    public async Task PlatformNotificationFailedEventHandler_Completes() =>
        await new PlatformNotificationFailedEventHandler(Mock.Of<ILogger<PlatformNotificationFailedEventHandler>>())
            .HandleAsync(new PlatformNotificationFailedEvent { OrderId = Guid.NewGuid(), PlatformCode = "TR", TrackingNumber = "T1", CargoProvider = CargoProvider.YurticiKargo, ErrorMessage = "Err", RetryCount = 1 }, default);

    [Fact]
    public async Task InvoiceApprovedEventHandler_Completes() =>
        await new InvoiceApprovedEventHandler(Mock.Of<ILogger<InvoiceApprovedEventHandler>>())
            .HandleAsync(new InvoiceApprovedEvent(Guid.NewGuid(), _t, "INV-001", 5000m, InvoiceType.EFatura, DateTime.UtcNow), default);
}
