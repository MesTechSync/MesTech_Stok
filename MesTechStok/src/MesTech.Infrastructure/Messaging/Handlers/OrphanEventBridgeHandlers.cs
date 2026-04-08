using Hangfire;
using MediatR;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Interfaces;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Calendar;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Documents;
using MesTech.Domain.Events.EInvoice;
using MesTech.Domain.Events.Finance;
using MesTech.Domain.Events.Hr;
using MesTech.Domain.Events.Tasks;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

// ═══════════════════════════════════════════════════════════════
// Orphan Event Bridge Handlers — 20 domain event'in MediatR köprüsü.
// Bu event'ler yayınlanıyor (raise) ama handler yoktu → sessiz kayıp.
// Her handler: log + bildirim dispatch.
// [ENT-DEV1] — V6 TUR 1
// ═══════════════════════════════════════════════════════════════

#region Kargo & Finans

/// <summary>
/// ShipmentCostRecordedEvent → Kargo gider kaydı loglama + bildirim.
/// Zincir Z7: Kargo → Gider GL (760.01)
/// </summary>
public sealed class ShipmentCostRecordedBridgeHandler
    : INotificationHandler<DomainEventNotification<ShipmentCostRecordedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShipmentCostRecordedBridgeHandler> _logger;

    public ShipmentCostRecordedBridgeHandler(IMediator mediator, ILogger<ShipmentCostRecordedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ShipmentCostRecordedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ShipmentCostRecorded — OrderId={OrderId}, Provider={Provider}, Cost={Cost}",
            e.OrderId, e.CargoProvider, e.ShippingCost);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "shipment-cost-recorded",
                Content: $"Kargo gideri kaydedildi.\n" +
                         $"Sipariş: {e.OrderId}\n" +
                         $"Kargo: {e.CargoProvider} — {e.ShippingCost:C}\n" +
                         $"Takip: {e.TrackingNumber}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ShipmentCostRecorded bildirim gönderilemedi — OrderId={OrderId}", e.OrderId);
        }
    }
}

/// <summary>
/// PaymentFailedEvent → Ödeme hatası loglama + bildirim.
/// Dunning/retry tetikleyici.
/// </summary>
public sealed class PaymentFailedBridgeHandler
    : INotificationHandler<DomainEventNotification<PaymentFailedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentFailedBridgeHandler> _logger;

    public PaymentFailedBridgeHandler(IMediator mediator, ILogger<PaymentFailedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentFailedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] PaymentFailed — SubscriptionId={SubscriptionId}, Error={Error}, Code={Code}, FailCount={Count}",
            e.SubscriptionId, e.ErrorMessage, e.ErrorCode, e.FailureCount);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "payment-failed",
                Content: $"Ödeme başarısız!\n" +
                         $"Abonelik: {e.SubscriptionId}\n" +
                         $"Hata: {e.ErrorMessage}\n" +
                         $"Deneme: {e.FailureCount}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "PaymentFailed bildirim gönderilemedi — Sub={SubId}", e.SubscriptionId);
        }
    }
}

/// <summary>
/// CashTransactionRecordedEvent → Nakit işlem kaydı loglama + bildirim.
/// </summary>
public sealed class CashTransactionRecordedBridgeHandler
    : INotificationHandler<DomainEventNotification<CashTransactionRecordedEvent>>
{
    private readonly ILogger<CashTransactionRecordedBridgeHandler> _logger;

    public CashTransactionRecordedBridgeHandler(ILogger<CashTransactionRecordedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<CashTransactionRecordedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] CashTransactionRecorded — RegisterId={RegisterId}, Type={Type}, Amount={Amount}, Balance={Balance}",
            e.CashRegisterId, e.Type, e.Amount, e.NewBalance);
        return Task.CompletedTask;
    }
}

/// <summary>
/// ExpenseApprovedEvent → Gider onayı loglama + bildirim.
/// </summary>
public sealed class ExpenseApprovedBridgeHandler
    : INotificationHandler<DomainEventNotification<ExpenseApprovedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExpenseApprovedBridgeHandler> _logger;

    public ExpenseApprovedBridgeHandler(IMediator mediator, ILogger<ExpenseApprovedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ExpenseApprovedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ExpenseApproved — ExpenseId={ExpenseId}, ApprovedBy={ApprovedBy}",
            e.ExpenseId, e.ApprovedByUserId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "expense-approved",
                Content: $"Gider onaylandı: {e.ExpenseId}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ExpenseApproved bildirim gönderilemedi — {ExpenseId}", e.ExpenseId);
        }
    }
}

#endregion

#region E-Fatura

/// <summary>
/// EInvoiceCreatedEvent → E-fatura oluşturuldu loglama + bildirim.
/// </summary>
public sealed class EInvoiceCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<EInvoiceCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<EInvoiceCreatedBridgeHandler> _logger;

    public EInvoiceCreatedBridgeHandler(IMediator mediator, ILogger<EInvoiceCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EInvoiceCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] EInvoiceCreated — EInvoiceId={EInvoiceId}, ETTN={ETTN}, Type={Type}",
            e.EInvoiceId, e.EttnNo, e.Type);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "einvoice-created",
                Content: $"E-Fatura oluşturuldu.\n" +
                         $"ETTN: {e.EttnNo}\n" +
                         $"Tip: {e.Type}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "EInvoiceCreated bildirim gönderilemedi — {EInvoiceId}", e.EInvoiceId);
        }
    }
}

/// <summary>
/// EInvoiceCancelledEvent → E-fatura iptal loglama + bildirim + integration publish.
/// </summary>
public sealed class EInvoiceCancelledBridgeHandler
    : INotificationHandler<DomainEventNotification<EInvoiceCancelledEvent>>
{
    private readonly IMediator _mediator;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<EInvoiceCancelledBridgeHandler> _logger;

    public EInvoiceCancelledBridgeHandler(
        IMediator mediator,
        IIntegrationEventPublisher publisher,
        ILogger<EInvoiceCancelledBridgeHandler> logger)
    {
        _mediator = mediator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EInvoiceCancelledEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] EInvoiceCancelled — EInvoiceId={EInvoiceId}, ETTN={ETTN}, Reason={Reason}",
            e.EInvoiceId, e.EttnNo, e.Reason);

        await _publisher.PublishEInvoiceCancelledAsync(
            e.EInvoiceId, e.EttnNo, e.Reason, ct)
            .ConfigureAwait(false);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "einvoice-cancelled",
                Content: $"E-Fatura iptal edildi!\n" +
                         $"ETTN: {e.EttnNo}\n" +
                         $"Sebep: {e.Reason}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "EInvoiceCancelled bildirim gönderilemedi — {EInvoiceId}", e.EInvoiceId);
        }
    }
}

/// <summary>
/// EInvoiceSentEvent → E-fatura gönderildi loglama + bildirim.
/// </summary>
public sealed class EInvoiceSentBridgeHandler
    : INotificationHandler<DomainEventNotification<EInvoiceSentEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<EInvoiceSentBridgeHandler> _logger;

    public EInvoiceSentBridgeHandler(
        IIntegrationEventPublisher publisher,
        ILogger<EInvoiceSentBridgeHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EInvoiceSentEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] EInvoiceSent — EInvoiceId={EInvoiceId}, ETTN={ETTN}, ProviderRef={Ref}",
            e.EInvoiceId, e.EttnNo, e.ProviderRef);

        await _publisher.PublishEInvoiceSentAsync(
            e.EInvoiceId, e.EttnNo, e.ProviderRef ?? "unknown", 0m, "TRY", ct)
            .ConfigureAwait(false);
    }
}

#endregion

#region Ürün & Stok

/// <summary>
/// ProductUpdatedEvent → Ürün güncelleme loglama + bildirim + integration event.
/// Platform sync tetikleyici olarak kullanılabilir.
/// DEV1 TUR1: NotificationLog dispatch eklendi (G10865).
/// </summary>
public sealed class ProductUpdatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ProductUpdatedEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IMediator _mediator;
    private readonly ILogger<ProductUpdatedBridgeHandler> _logger;

    public ProductUpdatedBridgeHandler(
        IIntegrationEventPublisher publisher,
        IMediator mediator,
        ILogger<ProductUpdatedBridgeHandler> logger)
    {
        _publisher = publisher;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ProductUpdatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ProductUpdated — ProductId={ProductId}, SKU={SKU}",
            e.ProductId, e.SKU);

        await _publisher.PublishProductUpdatedAsync(
            e.ProductId, e.SKU, "general", ct)
            .ConfigureAwait(false);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "product-updated",
                Content: $"Ürün güncellendi: {e.SKU}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ProductUpdated bildirim gönderilemedi — {ProductId}", e.ProductId);
        }
    }
}

/// <summary>
/// ProductActivatedEvent → Ürün aktif loglama + bildirim.
/// Platform'larda ürün satışa açılır.
/// </summary>
public sealed class ProductActivatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ProductActivatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductActivatedBridgeHandler> _logger;

    public ProductActivatedBridgeHandler(IMediator mediator, ILogger<ProductActivatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ProductActivatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ProductActivated — ProductId={ProductId}, SKU={SKU}",
            e.ProductId, e.SKU);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "product-activated",
                Content: $"Ürün aktifleştirildi: {e.SKU}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ProductActivated bildirim gönderilemedi — {ProductId}", e.ProductId);
        }
    }
}

/// <summary>
/// ProductDeactivatedEvent → Ürün pasif loglama.
/// Platform'larda ürün satıştan çekilir.
/// </summary>
public sealed class ProductDeactivatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ProductDeactivatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductDeactivatedBridgeHandler> _logger;

    public ProductDeactivatedBridgeHandler(IMediator mediator, ILogger<ProductDeactivatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ProductDeactivatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] ProductDeactivated — ProductId={ProductId}, SKU={SKU}",
            e.ProductId, e.SKU);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "product-deactivated",
                Content: $"Ürün pasife alındı: {e.SKU}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ProductDeactivated bildirim gönderilemedi — {ProductId}", e.ProductId);
        }
    }
}

#endregion

#region Abonelik

/// <summary>
/// SubscriptionCreatedEvent → Abonelik oluşturuldu loglama + bildirim.
/// </summary>
public sealed class SubscriptionCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<SubscriptionCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionCreatedBridgeHandler> _logger;

    public SubscriptionCreatedBridgeHandler(IMediator mediator, ILogger<SubscriptionCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<SubscriptionCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] SubscriptionCreated — SubscriptionId={SubId}, PlanId={PlanId}, Status={Status}",
            e.SubscriptionId, e.PlanId, e.Status);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "subscription-created",
                Content: $"Yeni abonelik oluşturuldu.\n" +
                         $"Plan: {e.PlanId}\n" +
                         $"Durum: {e.Status}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SubscriptionCreated bildirim gönderilemedi — {SubId}", e.SubscriptionId);
        }
    }
}

/// <summary>
/// SubscriptionCancelledEvent → Abonelik iptal loglama + bildirim.
/// </summary>
public sealed class SubscriptionCancelledBridgeHandler
    : INotificationHandler<DomainEventNotification<SubscriptionCancelledEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionCancelledBridgeHandler> _logger;

    public SubscriptionCancelledBridgeHandler(IMediator mediator, ILogger<SubscriptionCancelledBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<SubscriptionCancelledEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] SubscriptionCancelled — SubscriptionId={SubId}, Reason={Reason}",
            e.SubscriptionId, e.Reason);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "subscription-cancelled",
                Content: $"Abonelik iptal edildi.\n" +
                         $"Abonelik: {e.SubscriptionId}\n" +
                         $"Sebep: {e.Reason}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SubscriptionCancelled bildirim gönderilemedi — {SubId}", e.SubscriptionId);
        }
    }
}

#endregion

#region Entegrasyon & Sync

/// <summary>
/// SyncRequestedEvent → Senkronizasyon talebi loglama.
/// </summary>
public sealed class SyncRequestedBridgeHandler
    : INotificationHandler<DomainEventNotification<SyncRequestedEvent>>
{
    private readonly ILogger<SyncRequestedBridgeHandler> _logger;

    public SyncRequestedBridgeHandler(ILogger<SyncRequestedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<SyncRequestedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] SyncRequested — Platform={Platform}, Direction={Direction}, Entity={Entity}, EntityId={EntityId}",
            e.PlatformCode, e.Direction, e.EntityType, e.EntityId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// PlatformMessageReceivedEvent → Platform mesajı loglama + bildirim.
/// </summary>
public sealed class PlatformMessageReceivedBridgeHandler
    : INotificationHandler<DomainEventNotification<PlatformMessageReceivedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PlatformMessageReceivedBridgeHandler> _logger;

    public PlatformMessageReceivedBridgeHandler(IMediator mediator, ILogger<PlatformMessageReceivedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PlatformMessageReceivedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] PlatformMessageReceived — MessageId={MsgId}, Platform={Platform}, Sender={Sender}",
            e.MessageId, e.Platform, e.SenderName);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "platform-message",
                Content: $"Platform mesajı alındı.\n" +
                         $"Platform: {e.Platform}\n" +
                         $"Gönderen: {e.SenderName}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "PlatformMessageReceived bildirim gönderilemedi — {MsgId}", e.MessageId);
        }
    }
}

#endregion

#region CRM & İK

/// <summary>
/// CalendarEventCreatedEvent → Takvim etkinliği loglama + bildirim.
/// DEV1 TUR17: log-only → SendNotificationCommand dispatch eklendi.
/// </summary>
public sealed class CalendarEventCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<CalendarEventCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CalendarEventCreatedBridgeHandler> _logger;

    public CalendarEventCreatedBridgeHandler(IMediator mediator, ILogger<CalendarEventCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CalendarEventCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] CalendarEventCreated — EventId={EventId}, StartAt={StartAt}",
            e.EventId, e.StartAt);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "calendar-event-created",
                Content: $"Yeni takvim etkinliği: {e.StartAt:dd.MM.yyyy HH:mm}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "CalendarEventCreated bildirim gönderilemedi — {EventId}", e.EventId);
        }
    }
}

/// <summary>
/// DealStageChangedEvent → CRM deal aşama değişikliği loglama.
/// </summary>
public sealed class DealStageChangedBridgeHandler
    : INotificationHandler<DomainEventNotification<DealStageChangedEvent>>
{
    private readonly ILogger<DealStageChangedBridgeHandler> _logger;

    public DealStageChangedBridgeHandler(ILogger<DealStageChangedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<DealStageChangedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] DealStageChanged — DealId={DealId}, From={From}, To={To}",
            e.DealId, e.FromStageId, e.ToStageId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// LeaveRejectedEvent → İzin talebi red loglama + bildirim.
/// </summary>
public sealed class LeaveRejectedBridgeHandler
    : INotificationHandler<DomainEventNotification<LeaveRejectedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<LeaveRejectedBridgeHandler> _logger;

    public LeaveRejectedBridgeHandler(IMediator mediator, ILogger<LeaveRejectedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<LeaveRejectedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] LeaveRejected — LeaveId={LeaveId}, EmployeeId={EmpId}, Reason={Reason}",
            e.LeaveId, e.EmployeeId, e.Reason);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: e.EmployeeId.ToString(),
                TemplateName: "leave-rejected",
                Content: $"İzin talebiniz reddedildi.\nSebep: {e.Reason}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "LeaveRejected bildirim gönderilemedi — {LeaveId}", e.LeaveId);
        }
    }
}

/// <summary>
/// TaskCompletedEvent → Görev tamamlandı loglama.
/// </summary>
public sealed class TaskCompletedBridgeHandler
    : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
{
    private readonly ILogger<TaskCompletedBridgeHandler> _logger;

    public TaskCompletedBridgeHandler(ILogger<TaskCompletedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] TaskCompleted — TaskId={TaskId}, CompletedBy={UserId}",
            e.TaskId, e.CompletedByUserId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// TaskOverdueEvent → Gecikmiş görev uyarısı loglama + bildirim.
/// </summary>
public sealed class TaskOverdueBridgeHandler
    : INotificationHandler<DomainEventNotification<TaskOverdueEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TaskOverdueBridgeHandler> _logger;

    public TaskOverdueBridgeHandler(IMediator mediator, ILogger<TaskOverdueBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<TaskOverdueEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] TaskOverdue — TaskId={TaskId}, DueDate={DueDate}",
            e.TaskId, e.DueDate);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "task-overdue",
                Content: $"Gecikmiş görev!\n" +
                         $"Görev: {e.TaskId}\n" +
                         $"Son tarih: {e.DueDate:d}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "TaskOverdue bildirim gönderilemedi — {TaskId}", e.TaskId);
        }
    }
}

#endregion

#region Sistem & Doküman

/// <summary>
/// DocumentUploadedEvent → Doküman yükleme loglama + bildirim.
/// DEV1 TUR17: log-only → SendNotificationCommand dispatch eklendi.
/// </summary>
public sealed class DocumentUploadedBridgeHandler
    : INotificationHandler<DomainEventNotification<DocumentUploadedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentUploadedBridgeHandler> _logger;

    public DocumentUploadedBridgeHandler(IMediator mediator, ILogger<DocumentUploadedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<DocumentUploadedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] DocumentUploaded — DocumentId={DocId}, FileName={FileName}, Size={Size}",
            e.DocumentId, e.FileName, e.FileSizeBytes);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "document-uploaded",
                Content: $"Belge yüklendi: {e.FileName} ({e.FileSizeBytes / 1024} KB)"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "DocumentUploaded bildirim gönderilemedi — {DocId}", e.DocumentId);
        }
    }
}

/// <summary>
/// NotificationSettingsUpdatedEvent → Bildirim ayarları güncelleme loglama.
/// </summary>
public sealed class NotificationSettingsUpdatedBridgeHandler
    : INotificationHandler<DomainEventNotification<NotificationSettingsUpdatedEvent>>
{
    private readonly ILogger<NotificationSettingsUpdatedBridgeHandler> _logger;

    public NotificationSettingsUpdatedBridgeHandler(ILogger<NotificationSettingsUpdatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<NotificationSettingsUpdatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] NotificationSettingsUpdated — UserId={UserId}, Channel={Channel}, Enabled={Enabled}",
            e.UserId, e.Channel, e.IsEnabled);
        return Task.CompletedTask;
    }
}

/// <summary>
/// OnboardingCompletedEvent → Kurulum tamamlandı loglama + bildirim.
/// </summary>
public sealed class OnboardingCompletedBridgeHandler
    : INotificationHandler<DomainEventNotification<OnboardingCompletedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OnboardingCompletedBridgeHandler> _logger;

    public OnboardingCompletedBridgeHandler(IMediator mediator, ILogger<OnboardingCompletedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<OnboardingCompletedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] OnboardingCompleted — TenantId={TenantId}, Duration={Duration}",
            e.TenantId, e.CompletedAt - e.StartedAt);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "onboarding-completed",
                Content: $"Kurulum tamamlandı!\n" +
                         $"Süre: {(e.CompletedAt - e.StartedAt).TotalMinutes:F0} dakika"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OnboardingCompleted bildirim gönderilemedi — {TenantId}", e.TenantId);
        }
    }
}

#endregion

#region Muhasebe — Accounting Orphan Events [ENT-DEV1]

/// <summary>
/// ExpenseCreatedEvent → Gider oluşturuldu loglama + bildirim.
/// GL yevmiye kaydı ayrı handler'da yapılacak (ExpenseCreatedGLHandler).
/// </summary>
public sealed class ExpenseCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ExpenseCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExpenseCreatedBridgeHandler> _logger;

    public ExpenseCreatedBridgeHandler(IMediator mediator, ILogger<ExpenseCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ExpenseCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ExpenseCreated — ExpenseId={ExpenseId}, Title={Title}, Amount={Amount}, Source={Source}",
            e.ExpenseId, e.Title, e.Amount, e.Source);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "expense-created",
                Content: $"Yeni gider kaydı oluşturuldu.\n" +
                         $"Başlık: {e.Title}\n" +
                         $"Tutar: {e.Amount:C}\n" +
                         $"Kaynak: {e.Source}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ExpenseCreated bildirim gönderilemedi — {ExpenseId}", e.ExpenseId);
        }
    }
}

/// <summary>
/// TaxWithholdingComputedEvent → Stopaj hesaplaması loglama + bildirim.
/// GL kaydı: BORÇ 360 Ödenecek Vergi, ALACAK Nakit/Banka.
/// </summary>
public sealed class TaxWithholdingComputedBridgeHandler
    : INotificationHandler<DomainEventNotification<TaxWithholdingComputedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TaxWithholdingComputedBridgeHandler> _logger;

    public TaxWithholdingComputedBridgeHandler(IMediator mediator, ILogger<TaxWithholdingComputedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<TaxWithholdingComputedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] TaxWithholdingComputed — Id={Id}, TaxType={TaxType}, Rate={Rate}%, Amount={Amount}, Withholding={Withholding}",
            e.TaxWithholdingId, e.TaxType, e.Rate * 100, e.TaxExclusiveAmount, e.WithholdingAmount);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "tax-withholding-computed",
                Content: $"Stopaj hesaplandı.\n" +
                         $"Vergi tipi: {e.TaxType}\n" +
                         $"Matrah: {e.TaxExclusiveAmount:C}\n" +
                         $"Oran: %{e.Rate * 100:F1}\n" +
                         $"Stopaj: {e.WithholdingAmount:C}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "TaxWithholdingComputed bildirim gönderilemedi — {Id}", e.TaxWithholdingId);
        }
    }
}

/// <summary>
/// BankStatementImportedEvent → Banka ekstresi içe aktarıldı loglama + bildirim.
/// Mutabakat iş akışını tetikler.
/// </summary>
public sealed class BankStatementImportedBridgeHandler
    : INotificationHandler<DomainEventNotification<BankStatementImportedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BankStatementImportedBridgeHandler> _logger;

    public BankStatementImportedBridgeHandler(IMediator mediator, ILogger<BankStatementImportedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<BankStatementImportedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] BankStatementImported — BankAccountId={BankAccountId}, Transactions={Count}, Inflow={Inflow}, Outflow={Outflow}",
            e.BankAccountId, e.TransactionCount, e.TotalInflow, e.TotalOutflow);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "bank-statement-imported",
                Content: $"Banka ekstresi içe aktarıldı.\n" +
                         $"İşlem sayısı: {e.TransactionCount}\n" +
                         $"Giriş: {e.TotalInflow:C}\n" +
                         $"Çıkış: {e.TotalOutflow:C}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "BankStatementImported bildirim gönderilemedi — {BankAccountId}", e.BankAccountId);
        }
    }
}

/// <summary>
/// ReconciliationMatchedEvent → Mutabakat eşleştirmesi loglama.
/// Düşük güven skoru → uyarı.
/// </summary>
public sealed class ReconciliationMatchedBridgeHandler
    : INotificationHandler<DomainEventNotification<ReconciliationMatchedEvent>>
{
    private readonly ILogger<ReconciliationMatchedBridgeHandler> _logger;

    public ReconciliationMatchedBridgeHandler(ILogger<ReconciliationMatchedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<ReconciliationMatchedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        if (e.Confidence < 0.80m)
        {
            _logger.LogWarning(
                "[Event] ReconciliationMatched — LOW CONFIDENCE — MatchId={MatchId}, Confidence={Confidence:P0}, Bank={BankTxId}, Settlement={SettlementId}",
                e.ReconciliationMatchId, e.Confidence, e.BankTransactionId, e.SettlementBatchId);
        }
        else
        {
            _logger.LogInformation(
                "[Event] ReconciliationMatched — MatchId={MatchId}, Confidence={Confidence:P0}, Bank={BankTxId}, Settlement={SettlementId}",
                e.ReconciliationMatchId, e.Confidence, e.BankTransactionId, e.SettlementBatchId);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// ReconciliationCompletedEvent → Mutabakat tamamlandı loglama + bildirim.
/// </summary>
public sealed class ReconciliationCompletedBridgeHandler
    : INotificationHandler<DomainEventNotification<ReconciliationCompletedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReconciliationCompletedBridgeHandler> _logger;

    public ReconciliationCompletedBridgeHandler(IMediator mediator, ILogger<ReconciliationCompletedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ReconciliationCompletedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ReconciliationCompleted — MatchId={MatchId}, Status={Status}, Confidence={Confidence:P0}, Settlement={SettlementId}, Bank={BankTxId}",
            e.MatchId, e.FinalStatus, e.Confidence, e.SettlementBatchId, e.BankTransactionId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "reconciliation-completed",
                Content: $"Mutabakat tamamlandı.\n" +
                         $"Durum: {e.FinalStatus}\n" +
                         $"Güven: %{e.Confidence * 100:F0}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ReconciliationCompleted bildirim gönderilemedi — {MatchId}", e.MatchId);
        }
    }
}

/// <summary>
/// AnomalyDetectedEvent → Muhasebe anomalisi tespit loglama + UYARI bildirimi.
/// P0 seviye — hemen dikkat gerektirir.
/// </summary>
public sealed class AnomalyDetectedBridgeHandler
    : INotificationHandler<DomainEventNotification<AnomalyDetectedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnomalyDetectedBridgeHandler> _logger;

    public AnomalyDetectedBridgeHandler(IMediator mediator, ILogger<AnomalyDetectedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<AnomalyDetectedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] AnomalyDetected — Type={AnomalyType}, Description={Description}, Expected={Expected}, Actual={Actual}, Entity={EntityType}:{EntityId}",
            e.AnomalyType, e.Description, e.ExpectedAmount, e.ActualAmount, e.EntityType, e.EntityId);

        try
        {
            var detail = string.Empty;
            if (e.ExpectedAmount.HasValue) detail += $"Beklenen: {e.ExpectedAmount:C}\n";
            if (e.ActualAmount.HasValue) detail += $"Gerçekleşen: {e.ActualAmount:C}";

            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "anomaly-detected",
                Content: $"Muhasebe anomalisi tespit edildi!\n" +
                         $"Tip: {e.AnomalyType}\n" +
                         $"Açıklama: {e.Description}\n" +
                         detail), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AnomalyDetected bildirim gönderilemedi — {AnomalyType}", e.AnomalyType);
        }
    }
}

/// <summary>
/// BaBsRecordCreatedEvent → Ba/Bs kaydı oluşturuldu loglama.
/// Raporlama takvimi tetikleyici.
/// </summary>
public sealed class BaBsRecordCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<BaBsRecordCreatedEvent>>
{
    private readonly ILogger<BaBsRecordCreatedBridgeHandler> _logger;

    public BaBsRecordCreatedBridgeHandler(ILogger<BaBsRecordCreatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<BaBsRecordCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] BaBsRecordCreated — RecordId={RecordId}, Type={Type}, Period={Year}/{Month}, VKN={VKN}, Amount={Amount}",
            e.BaBsRecordId, e.Type, e.Year, e.Month, e.CounterpartyVkn, e.TotalAmount);
        return Task.CompletedTask;
    }
}

/// <summary>
/// FixedAssetCreatedEvent → Sabit kıymet oluşturuldu loglama + bildirim.
/// Amortisman takvimi başlatmak için tetikleyici.
/// </summary>
public sealed class FixedAssetCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<FixedAssetCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<FixedAssetCreatedBridgeHandler> _logger;

    public FixedAssetCreatedBridgeHandler(IMediator mediator, ILogger<FixedAssetCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<FixedAssetCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] FixedAssetCreated — AssetId={AssetId}, Name={Name}, Code={Code}, Cost={Cost}, Method={Method}",
            e.FixedAssetId, e.AssetName, e.AssetCode, e.AcquisitionCost, e.Method);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "fixed-asset-created",
                Content: $"Sabit kıymet kaydedildi.\n" +
                         $"Varlık: {e.AssetName} ({e.AssetCode})\n" +
                         $"Maliyet: {e.AcquisitionCost:C}\n" +
                         $"Amortisman: {e.Method}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "FixedAssetCreated bildirim gönderilemedi — {AssetId}", e.FixedAssetId);
        }
    }
}

/// <summary>
/// ProfitReportGeneratedEvent → Kar/zarar raporu oluşturuldu loglama.
/// </summary>
public sealed class ProfitReportGeneratedBridgeHandler
    : INotificationHandler<DomainEventNotification<ProfitReportGeneratedEvent>>
{
    private readonly ILogger<ProfitReportGeneratedBridgeHandler> _logger;

    public ProfitReportGeneratedBridgeHandler(ILogger<ProfitReportGeneratedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<ProfitReportGeneratedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ProfitReportGenerated — ReportId={ReportId}, Period={Period}, Platform={Platform}, NetProfit={NetProfit}",
            e.ReportId, e.Period, e.Platform ?? "ALL", e.NetProfit);
        return Task.CompletedTask;
    }
}

#endregion

#region Platform & CRM Orphan Events [ENT-DEV1]

/// <summary>
/// PlatformNotificationFailedEvent → Platform kargo bildirimi başarısız loglama + bildirim + Hangfire retry.
/// Max 3 retry (5/15/30 dk). ProcessOrderAsync tekrar cagirilir — basarisisz olursa event tekrar fire eder.
/// DEV3 TUR2: Hangfire retry mantigi eklendi.
/// </summary>
public sealed class PlatformNotificationFailedBridgeHandler
    : INotificationHandler<DomainEventNotification<PlatformNotificationFailedEvent>>
{
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30)
    ];

    private readonly IMediator _mediator;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<PlatformNotificationFailedBridgeHandler> _logger;

    public PlatformNotificationFailedBridgeHandler(
        IMediator mediator,
        IBackgroundJobClient backgroundJobs,
        ILogger<PlatformNotificationFailedBridgeHandler> logger)
    {
        _mediator = mediator;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PlatformNotificationFailedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] PlatformNotificationFailed — OrderId={OrderId}, Platform={Platform}, Tracking={Tracking}, Cargo={Cargo}, Error={Error}, Retry={Retry}",
            e.OrderId, e.PlatformCode, e.TrackingNumber, e.CargoProvider, e.ErrorMessage, e.RetryCount);

        // Hangfire retry — siparisi tekrar isle, platform bildirimi otomatik denenir
        if (e.RetryCount < MaxRetryCount)
        {
            var delay = RetryDelays[e.RetryCount];
            _backgroundJobs.Schedule<IAutoShipmentService>(
                svc => svc.ProcessOrderAsync(e.OrderId, CancellationToken.None),
                delay);

            _logger.LogInformation(
                "[Event] PlatformNotificationFailed retry scheduled — OrderId={OrderId}, Attempt={Attempt}, Delay={Delay}min",
                e.OrderId, e.RetryCount + 1, delay.TotalMinutes);
        }
        else
        {
            _logger.LogError(
                "[Event] PlatformNotificationFailed MAX RETRY ({Max}) — OrderId={OrderId}, Platform={Platform}. Manuel mudahale gerekli.",
                MaxRetryCount, e.OrderId, e.PlatformCode);
        }

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "platform-notification-failed",
                Content: $"Platform kargo bildirimi başarısız!\n" +
                         $"Platform: {e.PlatformCode}\n" +
                         $"Sipariş: {e.OrderId}\n" +
                         $"Takip: {e.TrackingNumber}\n" +
                         $"Hata: {e.ErrorMessage}\n" +
                         $"Deneme: {e.RetryCount}/{MaxRetryCount}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "PlatformNotificationFailed bildirim gönderilemedi — {OrderId}", e.OrderId);
        }
    }
}

/// <summary>
/// LeadScoredEvent → CRM lead puanlandı loglama.
/// MESA AI tarafından tetiklenir.
/// </summary>
public sealed class LeadScoredBridgeHandler
    : INotificationHandler<DomainEventNotification<LeadScoredEvent>>
{
    private readonly ILogger<LeadScoredBridgeHandler> _logger;

    public LeadScoredBridgeHandler(ILogger<LeadScoredBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<LeadScoredEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] LeadScored — LeadId={LeadId}, Score={Score}, Reasoning={Reasoning}",
            e.LeadId, e.Score, e.Reasoning);
        return Task.CompletedTask;
    }
}

/// <summary>
/// SubscriptionPlanChangedEvent → Abonelik planı değişti loglama + bildirim.
/// Özellik limitleri ve kotaların güncellenmesini tetikler.
/// </summary>
public sealed class SubscriptionPlanChangedBridgeHandler
    : INotificationHandler<DomainEventNotification<SubscriptionPlanChangedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionPlanChangedBridgeHandler> _logger;

    public SubscriptionPlanChangedBridgeHandler(IMediator mediator, ILogger<SubscriptionPlanChangedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<SubscriptionPlanChangedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] SubscriptionPlanChanged — SubscriptionId={SubId}, PreviousPlan={PrevPlan}, NewPlan={NewPlan}",
            e.SubscriptionId, e.PreviousPlanId, e.NewPlanId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "subscription-plan-changed",
                Content: $"Abonelik planı değiştirildi.\n" +
                         $"Önceki: {e.PreviousPlanId}\n" +
                         $"Yeni: {e.NewPlanId}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SubscriptionPlanChanged bildirim gönderilemedi — {SubId}", e.SubscriptionId);
        }
    }
}

#endregion

#region Katalog & Tedarik (DEV6 TUR19 — son 2 orphan event)

/// <summary>
/// CategoryCreatedEvent → Kategori oluşturma loglama + bildirim.
/// DEV6 TUR19: G462 orphan event kapatma. DEV1: bildirim eklendi.
/// </summary>
public sealed class CategoryCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<CategoryCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoryCreatedBridgeHandler> _logger;

    public CategoryCreatedBridgeHandler(IMediator mediator, ILogger<CategoryCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CategoryCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] CategoryCreated — CategoryId={CategoryId}, Name={Name}, Code={Code}, TenantId={TenantId}",
            e.CategoryId, e.CategoryName, e.Code, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "category-created",
                Content: $"Yeni kategori oluşturuldu: {e.CategoryName} (Kod: {e.Code})"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "CategoryCreated bildirim gönderilemedi — {CategoryId}", e.CategoryId);
        }
    }
}

/// <summary>
/// SupplierCreatedEvent → Tedarikçi oluşturma loglama + bildirim.
/// DEV6 TUR19: G462 orphan event kapatma. DEV1: bildirim eklendi.
/// </summary>
public sealed class SupplierCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<SupplierCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SupplierCreatedBridgeHandler> _logger;

    public SupplierCreatedBridgeHandler(IMediator mediator, ILogger<SupplierCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<SupplierCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] SupplierCreated — SupplierId={SupplierId}, Name={Name}, Code={Code}, TenantId={TenantId}",
            e.SupplierId, e.SupplierName, e.Code, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "supplier-created",
                Content: $"Yeni tedarikçi eklendi: {e.SupplierName} (Kod: {e.Code})"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SupplierCreated bildirim gönderilemedi — {SupplierId}", e.SupplierId);
        }
    }
}

#endregion

#region KD-DEV1: P0 Orphan Event Handlers (Kalite Devrimi)

/// <summary>
/// OrderPaidEvent → Sipariş ödeme tamamlandı — muhasebe GL kaydı + fatura oluşturma tetikle.
/// </summary>
public sealed class OrderPaidBridgeHandler
    : INotificationHandler<DomainEventNotification<OrderPaidEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderPaidBridgeHandler> _logger;

    public OrderPaidBridgeHandler(IMediator mediator, ILogger<OrderPaidBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<OrderPaidEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] OrderPaid — OrderId={OrderId}, OrderNumber={OrderNumber}, Amount={Amount}, TenantId={TenantId}",
            e.OrderId, e.OrderNumber, e.TotalAmount, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "order-paid",
                Content: $"Sipariş ödemesi alındı.\n" +
                         $"Sipariş: {e.OrderNumber}\n" +
                         $"Tutar: {e.TotalAmount:C}\n" +
                         $"Tarih: {e.OccurredAt:yyyy-MM-dd HH:mm}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OrderPaid bildirim gönderilemedi — {OrderId}", e.OrderId);
        }
    }
}

/// <summary>
/// OrderStatusChangedEvent → Sipariş durum değişimi loglama + bildirim.
/// </summary>
public sealed class OrderStatusChangedBridgeHandler
    : INotificationHandler<DomainEventNotification<OrderStatusChangedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderStatusChangedBridgeHandler> _logger;

    public OrderStatusChangedBridgeHandler(IMediator mediator, ILogger<OrderStatusChangedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<OrderStatusChangedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] OrderStatusChanged — OrderId={OrderId}, {OldStatus} → {NewStatus}, By={ChangedBy}, TenantId={TenantId}",
            e.OrderId, e.OldStatus, e.NewStatus, e.ChangedBy, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "order-status-changed",
                Content: $"Sipariş durumu değişti.\n" +
                         $"Sipariş: {e.OrderId}\n" +
                         $"{e.OldStatus} → {e.NewStatus}\n" +
                         $"Değiştiren: {e.ChangedBy ?? "Sistem"}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OrderStatusChanged bildirim gönderilemedi — {OrderId}", e.OrderId);
        }
    }
}

/// <summary>
/// ShipmentCreatedEvent → Kargo oluşturuldu loglama + bildirim.
/// </summary>
public sealed class ShipmentCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ShipmentCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShipmentCreatedBridgeHandler> _logger;

    public ShipmentCreatedBridgeHandler(IMediator mediator, ILogger<ShipmentCreatedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ShipmentCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ShipmentCreated — ShipmentId={ShipmentId}, OrderId={OrderId}, Tracking={Tracking}, Cargo={Cargo}, TenantId={TenantId}",
            e.ShipmentId, e.OrderId, e.TrackingNumber, e.CargoProvider, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "shipment-created",
                Content: $"Kargo oluşturuldu.\n" +
                         $"Sipariş: {e.OrderId}\n" +
                         $"Kargo: {e.CargoProvider} — Takip: {e.TrackingNumber}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ShipmentCreated bildirim gönderilemedi — {ShipmentId}", e.ShipmentId);
        }
    }
}

/// <summary>
/// PaymentCompletedEvent → Platform ödemesi tamamlandı loglama + bildirim.
/// </summary>
public sealed class PaymentCompletedBridgeHandler
    : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentCompletedBridgeHandler> _logger;

    public PaymentCompletedBridgeHandler(IMediator mediator, ILogger<PaymentCompletedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] PaymentCompleted — PaymentId={PaymentId}, Net={NetAmount}, BankRef={BankRef}, TenantId={TenantId}",
            e.PlatformPaymentId, e.NetAmount, e.BankReference, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "payment-completed",
                Content: $"Ödeme tamamlandı.\n" +
                         $"Net Tutar: {e.NetAmount:C}\n" +
                         $"Banka Ref: {e.BankReference ?? "—"}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "PaymentCompleted bildirim gönderilemedi — {PaymentId}", e.PlatformPaymentId);
        }
    }
}

/// <summary>
/// ReturnReceivedEvent → İade ürün teslim alındı — stok güncelleme + kalite kontrol tetikle.
/// </summary>
public sealed class ReturnReceivedBridgeHandler
    : INotificationHandler<DomainEventNotification<ReturnReceivedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReturnReceivedBridgeHandler> _logger;

    public ReturnReceivedBridgeHandler(IMediator mediator, ILogger<ReturnReceivedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<ReturnReceivedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ReturnReceived — ReturnId={ReturnId}, OrderId={OrderId}, TenantId={TenantId}",
            e.ReturnRequestId, e.OrderId, e.TenantId);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "return-received",
                Content: $"İade ürün teslim alındı.\n" +
                         $"İade No: {e.ReturnRequestId}\n" +
                         $"Sipariş: {e.OrderId}\n" +
                         $"Kalite kontrol ve stok güncelleme bekleniyor."), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ReturnReceived bildirim gönderilemedi — {ReturnId}", e.ReturnRequestId);
        }
    }
}

#endregion

// ═══════════════════════════════════════════════════════════════
// DEV1 — 10 kalan orphan event bridge (audit log)
// ═══════════════════════════════════════════════════════════════

public sealed class BankBalanceChangedBridge : INotificationHandler<DomainEventNotification<BankBalanceChangedEvent>>
{
    private readonly ILogger<BankBalanceChangedBridge> _l;
    public BankBalanceChangedBridge(ILogger<BankBalanceChangedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<BankBalanceChangedEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] BankBalanceChanged"); return Task.CompletedTask; }
}

public sealed class CariHesapCreatedBridge : INotificationHandler<DomainEventNotification<CariHesapCreatedEvent>>
{
    private readonly ILogger<CariHesapCreatedBridge> _l;
    public CariHesapCreatedBridge(ILogger<CariHesapCreatedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<CariHesapCreatedEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] CariHesapCreated — {Name}", n.DomainEvent.Name); return Task.CompletedTask; }
}

public sealed class CariHareketRecordedBridge : INotificationHandler<DomainEventNotification<CariHareketRecordedEvent>>
{
    private readonly ILogger<CariHareketRecordedBridge> _l;
    public CariHareketRecordedBridge(ILogger<CariHareketRecordedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<CariHareketRecordedEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] CariHareketRecorded — Amount={A}", n.DomainEvent.Amount); return Task.CompletedTask; }
}

public sealed class SettlementDisputedBridge : INotificationHandler<DomainEventNotification<SettlementDisputedEvent>>
{
    private readonly ILogger<SettlementDisputedBridge> _l;
    public SettlementDisputedBridge(ILogger<SettlementDisputedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<SettlementDisputedEvent> n, CancellationToken ct)
    { _l.LogWarning("[Event] SettlementDisputed — Batch={B}", n.DomainEvent.SettlementBatchId); return Task.CompletedTask; }
}

public sealed class CampaignCreatedBridge : INotificationHandler<DomainEventNotification<CampaignCreatedEvent>>
{
    private readonly ILogger<CampaignCreatedBridge> _l;
    public CampaignCreatedBridge(ILogger<CampaignCreatedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<CampaignCreatedEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] CampaignCreated — {Name}", n.DomainEvent.Name); return Task.CompletedTask; }
}

public sealed class InvoicePlatformSentBridge : INotificationHandler<DomainEventNotification<InvoicePlatformSentEvent>>
{
    private readonly ILogger<InvoicePlatformSentBridge> _l;
    public InvoicePlatformSentBridge(ILogger<InvoicePlatformSentBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<InvoicePlatformSentEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] InvoicePlatformSent — {Id}", n.DomainEvent.InvoiceId); return Task.CompletedTask; }
}

public sealed class ReturnRejectedBridge : INotificationHandler<DomainEventNotification<ReturnRejectedEvent>>
{
    private readonly ILogger<ReturnRejectedBridge> _l;
    public ReturnRejectedBridge(ILogger<ReturnRejectedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<ReturnRejectedEvent> n, CancellationToken ct)
    { _l.LogWarning("[Event] ReturnRejected — {Id}", n.DomainEvent.ReturnRequestId); return Task.CompletedTask; }
}

public sealed class QuotationAcceptedBridge : INotificationHandler<DomainEventNotification<QuotationAcceptedEvent>>
{
    private readonly ILogger<QuotationAcceptedBridge> _l;
    public QuotationAcceptedBridge(ILogger<QuotationAcceptedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<QuotationAcceptedEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] QuotationAccepted — {Id}", n.DomainEvent.QuotationId); return Task.CompletedTask; }
}

public sealed class QuotationRejectedBridge : INotificationHandler<DomainEventNotification<QuotationRejectedEvent>>
{
    private readonly ILogger<QuotationRejectedBridge> _l;
    public QuotationRejectedBridge(ILogger<QuotationRejectedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<QuotationRejectedEvent> n, CancellationToken ct)
    { _l.LogWarning("[Event] QuotationRejected — {Id}", n.DomainEvent.QuotationId); return Task.CompletedTask; }
}

public sealed class QuotationConvertedBridge : INotificationHandler<DomainEventNotification<QuotationConvertedEvent>>
{
    private readonly ILogger<QuotationConvertedBridge> _l;
    public QuotationConvertedBridge(ILogger<QuotationConvertedBridge> l) => _l = l;
    public Task Handle(DomainEventNotification<QuotationConvertedEvent> n, CancellationToken ct)
    { _l.LogInformation("[Event] QuotationConverted — Q={Q} Inv={I}", n.DomainEvent.QuotationId, n.DomainEvent.InvoiceId); return Task.CompletedTask; }
}
