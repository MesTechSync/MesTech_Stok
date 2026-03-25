using MediatR;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Calendar;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Documents;
using MesTech.Domain.Events.Finance;
using MesTech.Domain.Events.Hr;
using MesTech.Domain.Events.Tasks;
using MesTech.Infrastructure.Messaging.Mesa;

namespace MesTech.Infrastructure.Messaging.Handlers;

// ════════════════════════════════════════════════════════════════════════════
// APPLICATION EVENT BRIDGE HANDLERS — BATCH 2
// ════════════════════════════════════════════════════════════════════════════
//
// Batch 1 (ApplicationEventBridgeHandlers.cs): 10 kritik zincir handler
// Batch 2 (bu dosya): Kalan 35 DIRECT handler — event'i doğrudan geçen
//
// Pattern: Handler interface.HandleAsync(domainEvent, ct)
// ════════════════════════════════════════════════════════════════════════════

// ── Sipariş & Platform ──

public sealed class OrderReceivedApplicationBridge
    : INotificationHandler<DomainEventNotification<OrderReceivedEvent>>
{
    private readonly IOrderReceivedEventHandler _handler;
    public OrderReceivedApplicationBridge(IOrderReceivedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<OrderReceivedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class OrderShippedApplicationBridge
    : INotificationHandler<DomainEventNotification<OrderShippedEvent>>
{
    private readonly IOrderShippedEventHandler _handler;
    public OrderShippedApplicationBridge(IOrderShippedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<OrderShippedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ReturnCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<ReturnCreatedEvent>>
{
    private readonly IReturnCreatedEventHandler _handler;
    public ReturnCreatedApplicationBridge(IReturnCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ReturnCreatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ReturnResolvedApplicationBridge
    : INotificationHandler<DomainEventNotification<ReturnResolvedEvent>>
{
    private readonly IReturnResolvedEventHandler _handler;
    public ReturnResolvedApplicationBridge(IReturnResolvedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ReturnResolvedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── Ürün & Stok ──

public sealed class ProductCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<ProductCreatedEvent>>
{
    private readonly IProductCreatedEventHandler _handler;
    public ProductCreatedApplicationBridge(IProductCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ProductCreatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ProductUpdatedApplicationBridge
    : INotificationHandler<DomainEventNotification<ProductUpdatedEvent>>
{
    private readonly IProductUpdatedEventHandler _handler;
    public ProductUpdatedApplicationBridge(IProductUpdatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ProductUpdatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class PriceChangedApplicationBridge
    : INotificationHandler<DomainEventNotification<PriceChangedEvent>>
{
    private readonly IPriceChangedEventHandler _handler;
    public PriceChangedApplicationBridge(IPriceChangedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<PriceChangedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class StockCriticalApplicationBridge
    : INotificationHandler<DomainEventNotification<StockCriticalEvent>>
{
    private readonly IStockCriticalEventHandler _handler;
    public StockCriticalApplicationBridge(IStockCriticalEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<StockCriticalEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class BuyboxLostApplicationBridge
    : INotificationHandler<DomainEventNotification<BuyboxLostEvent>>
{
    private readonly IBuyboxLostEventHandler _handler;
    public BuyboxLostApplicationBridge(IBuyboxLostEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<BuyboxLostEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── Fatura & Muhasebe ──

public sealed class InvoiceApprovedApplicationBridge
    : INotificationHandler<DomainEventNotification<InvoiceApprovedEvent>>
{
    private readonly IInvoiceApprovedEventHandler _handler;
    public InvoiceApprovedApplicationBridge(IInvoiceApprovedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<InvoiceApprovedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class InvoiceGeneratedForERPApplicationBridge
    : INotificationHandler<DomainEventNotification<InvoiceGeneratedForERPEvent>>
{
    private readonly IInvoiceGeneratedForERPEventHandler _handler;
    public InvoiceGeneratedForERPApplicationBridge(IInvoiceGeneratedForERPEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<InvoiceGeneratedForERPEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class BaBsRecordCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<BaBsRecordCreatedEvent>>
{
    private readonly IBaBsRecordCreatedEventHandler _handler;
    public BaBsRecordCreatedApplicationBridge(IBaBsRecordCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<BaBsRecordCreatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class BankStatementImportedApplicationBridge
    : INotificationHandler<DomainEventNotification<BankStatementImportedEvent>>
{
    private readonly IBankStatementImportedEventHandler _handler;
    public BankStatementImportedApplicationBridge(IBankStatementImportedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<BankStatementImportedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class CashTransactionRecordedApplicationBridge
    : INotificationHandler<DomainEventNotification<CashTransactionRecordedEvent>>
{
    private readonly ICashTransactionRecordedEventHandler _handler;
    public CashTransactionRecordedApplicationBridge(ICashTransactionRecordedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<CashTransactionRecordedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ExpenseCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<ExpenseCreatedEvent>>
{
    private readonly IExpenseCreatedEventHandler _handler;
    public ExpenseCreatedApplicationBridge(IExpenseCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ExpenseCreatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ExpensePaidApplicationBridge
    : INotificationHandler<DomainEventNotification<ExpensePaidEvent>>
{
    private readonly IExpensePaidEventHandler _handler;
    public ExpensePaidApplicationBridge(IExpensePaidEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ExpensePaidEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class FixedAssetCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<FixedAssetCreatedEvent>>
{
    private readonly IFixedAssetCreatedEventHandler _handler;
    public FixedAssetCreatedApplicationBridge(IFixedAssetCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<FixedAssetCreatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ProfitReportGeneratedApplicationBridge
    : INotificationHandler<DomainEventNotification<ProfitReportGeneratedEvent>>
{
    private readonly IProfitReportGeneratedEventHandler _handler;
    public ProfitReportGeneratedApplicationBridge(IProfitReportGeneratedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ProfitReportGeneratedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ReconciliationCompletedApplicationBridge
    : INotificationHandler<DomainEventNotification<ReconciliationCompletedEvent>>
{
    private readonly IReconciliationCompletedEventHandler _handler;
    public ReconciliationCompletedApplicationBridge(IReconciliationCompletedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ReconciliationCompletedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class ReconciliationMatchedApplicationBridge
    : INotificationHandler<DomainEventNotification<ReconciliationMatchedEvent>>
{
    private readonly IReconciliationMatchedEventHandler _handler;
    public ReconciliationMatchedApplicationBridge(IReconciliationMatchedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ReconciliationMatchedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class TaxWithholdingComputedApplicationBridge
    : INotificationHandler<DomainEventNotification<TaxWithholdingComputedEvent>>
{
    private readonly ITaxWithholdingComputedEventHandler _handler;
    public TaxWithholdingComputedApplicationBridge(ITaxWithholdingComputedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<TaxWithholdingComputedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class DailySummaryGeneratedApplicationBridge
    : INotificationHandler<DomainEventNotification<DailySummaryGeneratedEvent>>
{
    private readonly IDailySummaryGeneratedEventHandler _handler;
    public DailySummaryGeneratedApplicationBridge(IDailySummaryGeneratedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<DailySummaryGeneratedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── Sync & Platform Notification ──

public sealed class SyncRequestedApplicationBridge
    : INotificationHandler<DomainEventNotification<SyncRequestedEvent>>
{
    private readonly ISyncRequestedEventHandler _handler;
    public SyncRequestedApplicationBridge(ISyncRequestedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<SyncRequestedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class SyncErrorOccurredApplicationBridge
    : INotificationHandler<DomainEventNotification<SyncErrorOccurredEvent>>
{
    private readonly ISyncErrorOccurredEventHandler _handler;
    public SyncErrorOccurredApplicationBridge(ISyncErrorOccurredEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<SyncErrorOccurredEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class SupplierFeedSyncedApplicationBridge
    : INotificationHandler<DomainEventNotification<SupplierFeedSyncedEvent>>
{
    private readonly ISupplierFeedSyncedEventHandler _handler;
    public SupplierFeedSyncedApplicationBridge(ISupplierFeedSyncedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<SupplierFeedSyncedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class PlatformNotificationFailedApplicationBridge
    : INotificationHandler<DomainEventNotification<PlatformNotificationFailedEvent>>
{
    private readonly IPlatformNotificationFailedEventHandler _handler;
    public PlatformNotificationFailedApplicationBridge(IPlatformNotificationFailedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<PlatformNotificationFailedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── CRM ──

public sealed class DealLostApplicationBridge
    : INotificationHandler<DomainEventNotification<DealLostEvent>>
{
    private readonly IDealLostEventHandler _handler;
    public DealLostApplicationBridge(IDealLostEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<DealLostEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class DealStageChangedApplicationBridge
    : INotificationHandler<DomainEventNotification<DealStageChangedEvent>>
{
    private readonly IDealStageChangedEventHandler _handler;
    public DealStageChangedApplicationBridge(IDealStageChangedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<DealStageChangedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class DealWonApplicationBridge
    : INotificationHandler<DomainEventNotification<DealWonEvent>>
{
    private readonly IDealWonEventHandler _handler;
    public DealWonApplicationBridge(IDealWonEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<DealWonEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class LeadConvertedApplicationBridge
    : INotificationHandler<DomainEventNotification<LeadConvertedEvent>>
{
    private readonly ILeadConvertedEventHandler _handler;
    public LeadConvertedApplicationBridge(ILeadConvertedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<LeadConvertedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── HR ──

public sealed class LeaveApprovedApplicationBridge
    : INotificationHandler<DomainEventNotification<LeaveApprovedEvent>>
{
    private readonly ILeaveApprovedEventHandler _handler;
    public LeaveApprovedApplicationBridge(ILeaveApprovedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<LeaveApprovedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── Documents ──

public sealed class DocumentUploadedApplicationBridge
    : INotificationHandler<DomainEventNotification<DocumentUploadedEvent>>
{
    private readonly IDocumentUploadedEventHandler _handler;
    public DocumentUploadedApplicationBridge(IDocumentUploadedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<DocumentUploadedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── Calendar ──

public sealed class CalendarEventCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<CalendarEventCreatedEvent>>
{
    private readonly ICalendarEventCreatedEventHandler _handler;
    public CalendarEventCreatedApplicationBridge(ICalendarEventCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<CalendarEventCreatedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

// ── Tasks ──

public sealed class TaskCompletedApplicationBridge
    : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
{
    private readonly ITaskCompletedEventHandler _handler;
    public TaskCompletedApplicationBridge(ITaskCompletedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<TaskCompletedEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}

public sealed class TaskOverdueApplicationBridge
    : INotificationHandler<DomainEventNotification<TaskOverdueEvent>>
{
    private readonly ITaskOverdueEventHandler _handler;
    public TaskOverdueApplicationBridge(ITaskOverdueEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<TaskOverdueEvent> n, CancellationToken ct)
        => _handler.HandleAsync(n.DomainEvent, ct);
}
