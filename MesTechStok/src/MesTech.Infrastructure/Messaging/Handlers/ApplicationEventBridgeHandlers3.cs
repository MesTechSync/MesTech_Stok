using MediatR;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Events;
using MesTech.Domain.Events.EInvoice;
using MesTech.Domain.Events.Finance;
using MesTech.Domain.Events.Hr;
using MesTech.Infrastructure.Messaging.Mesa;

namespace MesTech.Infrastructure.Messaging.Handlers;

// ════════════════════════════════════════════════════════════════════════════
// APPLICATION EVENT BRIDGE HANDLERS — BATCH 3
// ════════════════════════════════════════════════════════════════════════════
//
// Batch 1: 10 kritik zincir handler (Z1,Z2,Z5,Z7,Z8,Z10,Z11)
// Batch 2: 35 DIRECT handler
// Batch 3 (bu dosya): 11 PARAMS handler — event property'lerinden parametre çekilen
// ════════════════════════════════════════════════════════════════════════════

// ── E-Fatura ──

public sealed class EInvoiceCancelledApplicationBridge
    : INotificationHandler<DomainEventNotification<EInvoiceCancelledEvent>>
{
    private readonly IEInvoiceCancelledEventHandler _handler;
    public EInvoiceCancelledApplicationBridge(IEInvoiceCancelledEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<EInvoiceCancelledEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.EInvoiceId, e.EttnNo, e.Reason, ct);
    }
}

public sealed class EInvoiceCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<EInvoiceCreatedEvent>>
{
    private readonly IEInvoiceCreatedEventHandler _handler;
    public EInvoiceCreatedApplicationBridge(IEInvoiceCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<EInvoiceCreatedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.EInvoiceId, e.EttnNo, e.Type, ct);
    }
}

public sealed class EInvoiceSentApplicationBridge
    : INotificationHandler<DomainEventNotification<EInvoiceSentEvent>>
{
    private readonly IEInvoiceSentEventHandler _handler;
    public EInvoiceSentApplicationBridge(IEInvoiceSentEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<EInvoiceSentEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.EInvoiceId, e.EttnNo, e.ProviderRef, ct);
    }
}

// ── Finance ──

public sealed class ExpenseApprovedApplicationBridge
    : INotificationHandler<DomainEventNotification<ExpenseApprovedEvent>>
{
    private readonly IExpenseApprovedEventHandler _handler;
    public ExpenseApprovedApplicationBridge(IExpenseApprovedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<ExpenseApprovedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.ExpenseId, e.ApprovedByUserId, ct);
    }
}

// ── HR ──

public sealed class LeaveRejectedApplicationBridge
    : INotificationHandler<DomainEventNotification<LeaveRejectedEvent>>
{
    private readonly ILeaveRejectedEventHandler _handler;
    public LeaveRejectedApplicationBridge(ILeaveRejectedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<LeaveRejectedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.LeaveId, e.EmployeeId, e.Reason, ct);
    }
}

// ── Notification ──

public sealed class NotificationSettingsUpdatedApplicationBridge
    : INotificationHandler<DomainEventNotification<NotificationSettingsUpdatedEvent>>
{
    private readonly INotificationSettingsUpdatedEventHandler _handler;
    public NotificationSettingsUpdatedApplicationBridge(INotificationSettingsUpdatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<NotificationSettingsUpdatedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.UserId, e.Channel, e.IsEnabled, ct);
    }
}

// ── Onboarding ──

public sealed class OnboardingCompletedApplicationBridge
    : INotificationHandler<DomainEventNotification<OnboardingCompletedEvent>>
{
    private readonly IOnboardingCompletedEventHandler _handler;
    public OnboardingCompletedApplicationBridge(IOnboardingCompletedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<OnboardingCompletedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.TenantId, e.OnboardingProgressId, e.StartedAt, e.CompletedAt, ct);
    }
}

// ── Billing ──

public sealed class PaymentFailedApplicationBridge
    : INotificationHandler<DomainEventNotification<PaymentFailedEvent>>
{
    private readonly IPaymentFailedEventHandler _handler;
    public PaymentFailedApplicationBridge(IPaymentFailedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<PaymentFailedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.TenantId, e.SubscriptionId, e.ErrorMessage, e.ErrorCode, e.FailureCount, ct);
    }
}

public sealed class SubscriptionCancelledApplicationBridge
    : INotificationHandler<DomainEventNotification<SubscriptionCancelledEvent>>
{
    private readonly ISubscriptionCancelledEventHandler _handler;
    public SubscriptionCancelledApplicationBridge(ISubscriptionCancelledEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<SubscriptionCancelledEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.TenantId, e.SubscriptionId, e.Reason, ct);
    }
}

public sealed class SubscriptionCreatedApplicationBridge
    : INotificationHandler<DomainEventNotification<SubscriptionCreatedEvent>>
{
    private readonly ISubscriptionCreatedEventHandler _handler;
    public SubscriptionCreatedApplicationBridge(ISubscriptionCreatedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<SubscriptionCreatedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.TenantId, e.SubscriptionId, e.PlanId, ct);
    }
}

// ── Platform ──

public sealed class PlatformMessageReceivedApplicationBridge
    : INotificationHandler<DomainEventNotification<PlatformMessageReceivedEvent>>
{
    private readonly IPlatformMessageReceivedEventHandler _handler;
    public PlatformMessageReceivedApplicationBridge(IPlatformMessageReceivedEventHandler handler) => _handler = handler;
    public Task Handle(DomainEventNotification<PlatformMessageReceivedEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return _handler.HandleAsync(e.MessageId, e.Platform, e.SenderName, ct);
    }
}
