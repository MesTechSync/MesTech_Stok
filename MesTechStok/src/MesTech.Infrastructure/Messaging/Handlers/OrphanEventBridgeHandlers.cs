using MediatR;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
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
/// EInvoiceCancelledEvent → E-fatura iptal loglama + bildirim.
/// </summary>
public sealed class EInvoiceCancelledBridgeHandler
    : INotificationHandler<DomainEventNotification<EInvoiceCancelledEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<EInvoiceCancelledBridgeHandler> _logger;

    public EInvoiceCancelledBridgeHandler(IMediator mediator, ILogger<EInvoiceCancelledBridgeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EInvoiceCancelledEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] EInvoiceCancelled — EInvoiceId={EInvoiceId}, ETTN={ETTN}, Reason={Reason}",
            e.EInvoiceId, e.EttnNo, e.Reason);

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
    private readonly ILogger<EInvoiceSentBridgeHandler> _logger;

    public EInvoiceSentBridgeHandler(ILogger<EInvoiceSentBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<EInvoiceSentEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] EInvoiceSent — EInvoiceId={EInvoiceId}, ETTN={ETTN}, ProviderRef={Ref}",
            e.EInvoiceId, e.EttnNo, e.ProviderRef);
        return Task.CompletedTask;
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
    private readonly ILogger<ProductUpdatedBridgeHandler> _logger;

    public ProductUpdatedBridgeHandler(ILogger<ProductUpdatedBridgeHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<ProductUpdatedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] ProductUpdated — ProductId={ProductId}, SKU={SKU}",
            e.ProductId, e.SKU);
        return Task.CompletedTask;
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
