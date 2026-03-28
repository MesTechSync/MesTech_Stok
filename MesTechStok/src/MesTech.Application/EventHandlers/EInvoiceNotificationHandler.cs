using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// E-fatura yaşam döngüsü bildirimleri — Created/Sent/Cancelled.
/// EInvoiceCreatedEvent, EInvoiceSentEvent, EInvoiceCancelledEvent → NotificationLog.
/// </summary>
public interface IEInvoiceNotificationHandler
{
    Task HandleCreatedAsync(Guid eInvoiceId, Guid tenantId, string ettnNo, CancellationToken ct);
    Task HandleSentAsync(Guid eInvoiceId, Guid tenantId, string ettnNo, string? providerRef, CancellationToken ct);
    Task HandleCancelledAsync(Guid eInvoiceId, Guid tenantId, string ettnNo, string reason, CancellationToken ct);
}

public sealed class EInvoiceNotificationHandler : IEInvoiceNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EInvoiceNotificationHandler> _logger;

    public EInvoiceNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<EInvoiceNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleCreatedAsync(Guid eInvoiceId, Guid tenantId, string ettnNo, CancellationToken ct)
    {
        _logger.LogInformation("EInvoiceCreated → bildirim. ETTN={ETTN}", ettnNo);
        await CreateNotificationAsync(tenantId, "EInvoiceCreated",
            $"E-Fatura oluşturuldu — ETTN: {ettnNo}", ct);
    }

    public async Task HandleSentAsync(Guid eInvoiceId, Guid tenantId, string ettnNo, string? providerRef, CancellationToken ct)
    {
        _logger.LogInformation("EInvoiceSent → bildirim. ETTN={ETTN}, Ref={Ref}", ettnNo, providerRef);
        await CreateNotificationAsync(tenantId, "EInvoiceSent",
            $"E-Fatura gönderildi — ETTN: {ettnNo}, Provider Ref: {providerRef ?? "N/A"}", ct);
    }

    public async Task HandleCancelledAsync(Guid eInvoiceId, Guid tenantId, string ettnNo, string reason, CancellationToken ct)
    {
        _logger.LogWarning("EInvoiceCancelled → bildirim. ETTN={ETTN}, Reason={Reason}", ettnNo, reason);
        await CreateNotificationAsync(tenantId, "EInvoiceCancelled",
            $"E-Fatura iptal edildi — ETTN: {ettnNo}, Sebep: {reason}", ct);
    }

    private async Task CreateNotificationAsync(Guid tenantId, string template, string content, CancellationToken ct)
    {
        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            template,
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
