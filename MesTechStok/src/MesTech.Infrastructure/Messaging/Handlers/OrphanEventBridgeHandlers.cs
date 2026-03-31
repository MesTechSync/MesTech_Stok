using MediatR;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
/// ProductUpdatedEvent → Ürün güncelleme loglama.
/// Platform sync tetikleyici olarak kullanılabilir.
/// </summary>
public sealed class ProductUpdatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ProductUpdatedEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<ProductUpdatedBridgeHandler> _logger;

    public ProductUpdatedBridgeHandler(
        IIntegrationEventPublisher publisher,
        ILogger<ProductUpdatedBridgeHandler> logger)
    {
        _publisher = publisher;
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
    }
}

/// <summary>
/// ProductActivatedEvent → Ürün aktif loglama.
/// Platform'larda ürün satışa açılır.
/// </summary>
public sealed class ProductActivatedBridgeHandler
    : INotificationHandler<DomainEventNotification<ProductActivatedEvent>>
{
    private readonly ILogger<ProductActivatedBridgeHandler> _logger;

    public ProductActivatedBridgeHandler(ILogger<ProductActivatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<ProductActivatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ProductActivated — ProductId={ProductId}, SKU={SKU}",
            e.ProductId, e.SKU);
        return Task.CompletedTask;
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PlatformMessageReceived bildirim gönderilemedi — {MsgId}", e.MessageId);
        }
    }
}

#endregion

#region CRM & İK

/// <summary>
/// CalendarEventCreatedEvent → Takvim etkinliği loglama.
/// </summary>
public sealed class CalendarEventCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<CalendarEventCreatedEvent>>
{
    private readonly ILogger<CalendarEventCreatedBridgeHandler> _logger;

    public CalendarEventCreatedBridgeHandler(ILogger<CalendarEventCreatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<CalendarEventCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] CalendarEventCreated — EventId={EventId}, StartAt={StartAt}",
            e.EventId, e.StartAt);
        return Task.CompletedTask;
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
        catch (Exception ex)
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TaskOverdue bildirim gönderilemedi — {TaskId}", e.TaskId);
        }
    }
}

#endregion

#region Sistem & Doküman

/// <summary>
/// DocumentUploadedEvent → Doküman yükleme loglama.
/// </summary>
public sealed class DocumentUploadedBridgeHandler
    : INotificationHandler<DomainEventNotification<DocumentUploadedEvent>>
{
    private readonly ILogger<DocumentUploadedBridgeHandler> _logger;

    public DocumentUploadedBridgeHandler(ILogger<DocumentUploadedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<DocumentUploadedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] DocumentUploaded — DocumentId={DocId}, FileName={FileName}, Size={Size}",
            e.DocumentId, e.FileName, e.FileSizeBytes);
        return Task.CompletedTask;
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
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
/// PlatformNotificationFailedEvent → Platform kargo bildirimi başarısız loglama + bildirim.
/// Hangfire retry tetikleyici.
/// </summary>
public sealed class PlatformNotificationFailedBridgeHandler
    : INotificationHandler<DomainEventNotification<PlatformNotificationFailedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PlatformNotificationFailedBridgeHandler> _logger;

    public PlatformNotificationFailedBridgeHandler(IMediator mediator, ILogger<PlatformNotificationFailedBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PlatformNotificationFailedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] PlatformNotificationFailed — OrderId={OrderId}, Platform={Platform}, Tracking={Tracking}, Cargo={Cargo}, Error={Error}, Retry={Retry}",
            e.OrderId, e.PlatformCode, e.TrackingNumber, e.CargoProvider, e.ErrorMessage, e.RetryCount);

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
                         $"Deneme: {e.RetryCount}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SubscriptionPlanChanged bildirim gönderilemedi — {SubId}", e.SubscriptionId);
        }
    }
}

#endregion

#region Katalog & Tedarik (DEV6 TUR19 — son 2 orphan event)

/// <summary>
/// CategoryCreatedEvent → Kategori oluşturma loglama.
/// DEV6 TUR19: G462 orphan event kapatma.
/// </summary>
public sealed class CategoryCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<CategoryCreatedEvent>>
{
    private readonly ILogger<CategoryCreatedBridgeHandler> _logger;

    public CategoryCreatedBridgeHandler(ILogger<CategoryCreatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<CategoryCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] CategoryCreated — CategoryId={CategoryId}, Name={Name}, Code={Code}, TenantId={TenantId}",
            e.CategoryId, e.CategoryName, e.Code, e.TenantId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// SupplierCreatedEvent → Tedarikçi oluşturma loglama.
/// DEV6 TUR19: G462 orphan event kapatma.
/// </summary>
public sealed class SupplierCreatedBridgeHandler
    : INotificationHandler<DomainEventNotification<SupplierCreatedEvent>>
{
    private readonly ILogger<SupplierCreatedBridgeHandler> _logger;

    public SupplierCreatedBridgeHandler(ILogger<SupplierCreatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<SupplierCreatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] SupplierCreated — SupplierId={SupplierId}, Name={Name}, Code={Code}, TenantId={TenantId}",
            e.SupplierId, e.SupplierName, e.Code, e.TenantId);
        return Task.CompletedTask;
    }
}

#endregion
