using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Fatura yaşam döngüsü bildirimleri — Accepted/Rejected/Sent/GeneratedForERP.
/// Tek handler, çoklu event — aynı pattern (NotificationLog).
/// </summary>
public interface IInvoiceLifecycleNotificationHandler
{
    Task HandleAcceptedAsync(Guid invoiceId, Guid tenantId, string invoiceNumber, decimal grandTotal, CancellationToken ct);
    Task HandleRejectedAsync(Guid invoiceId, Guid tenantId, string invoiceNumber, CancellationToken ct);
    Task HandleSentAsync(Guid invoiceId, Guid tenantId, Guid orderId, string? gibInvoiceId, CancellationToken ct);
    Task HandleGeneratedForERPAsync(Guid invoiceId, Guid tenantId, CancellationToken ct);
}

public sealed class InvoiceLifecycleNotificationHandler : IInvoiceLifecycleNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InvoiceLifecycleNotificationHandler> _logger;

    public InvoiceLifecycleNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<InvoiceLifecycleNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAcceptedAsync(Guid invoiceId, Guid tenantId, string invoiceNumber, decimal grandTotal, CancellationToken ct)
    {
        _logger.LogInformation("InvoiceAccepted → bildirim. Invoice={Inv}, Total={Total}", invoiceNumber, grandTotal);
        await CreateNotificationAsync(tenantId, "InvoiceAccepted",
            $"Fatura kabul edildi — #{invoiceNumber}, Tutar: {grandTotal:C2}", ct);
    }

    public async Task HandleRejectedAsync(Guid invoiceId, Guid tenantId, string invoiceNumber, CancellationToken ct)
    {
        _logger.LogWarning("InvoiceRejected → bildirim. Invoice={Inv}", invoiceNumber);
        await CreateNotificationAsync(tenantId, "InvoiceRejected",
            $"Fatura reddedildi — #{invoiceNumber}. Yeniden düzenleme gerekli.", ct);
    }

    public async Task HandleSentAsync(Guid invoiceId, Guid tenantId, Guid orderId, string? gibInvoiceId, CancellationToken ct)
    {
        _logger.LogInformation("InvoiceSent → bildirim. InvoiceId={Id}, GIB={GIB}", invoiceId, gibInvoiceId);
        await CreateNotificationAsync(tenantId, "InvoiceSent",
            $"Fatura gönderildi — GİB No: {gibInvoiceId ?? "bekliyor"}", ct);
    }

    public async Task HandleGeneratedForERPAsync(Guid invoiceId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation("InvoiceGeneratedForERP → bildirim. InvoiceId={Id}", invoiceId);
        await CreateNotificationAsync(tenantId, "InvoiceGeneratedForERP",
            $"Fatura ERP için hazırlandı — ID: {invoiceId}", ct);
    }

    private async Task CreateNotificationAsync(Guid tenantId, string template, string content, CancellationToken ct)
    {
        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            template,
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
