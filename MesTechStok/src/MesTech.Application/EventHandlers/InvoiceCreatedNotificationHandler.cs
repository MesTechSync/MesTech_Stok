using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Fatura oluşturulduğunda bildirim kaydı oluşturur.
/// InvoiceCreatedEvent → NotificationLog (fatura hazır, onay bekliyor).
/// GL kaydı InvoiceApprovedGLHandler tarafından onay aşamasında oluşturulur.
/// </summary>
public interface IInvoiceCreatedNotificationHandler
{
    Task HandleAsync(
        Guid invoiceId, Guid orderId, Guid tenantId,
        decimal grandTotal,
        CancellationToken ct);
}

public sealed class InvoiceCreatedNotificationHandler : IInvoiceCreatedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InvoiceCreatedNotificationHandler> _logger;

    public InvoiceCreatedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<InvoiceCreatedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid invoiceId, Guid orderId, Guid tenantId,
        decimal grandTotal,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "InvoiceCreated → bildirim oluşturuluyor. InvoiceId={InvoiceId}, OrderId={OrderId}, Total={Total}",
            invoiceId, orderId, grandTotal);

        var content = $"Yeni fatura oluşturuldu — Tutar: {grandTotal:C2}. Onay bekliyor.";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "InvoiceCreated",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Fatura bildirimi oluşturuldu — InvoiceId={InvoiceId}", invoiceId);
    }
}
