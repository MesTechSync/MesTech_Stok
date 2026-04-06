using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş tamamlandığında otomatik e-arşiv fatura oluşturur (Zincir 2b).
/// OrderCompletedEvent → Invoice.CreateForOrder → InvoiceCreatedEvent → (onay akışı).
/// Fatura kesilmeden muhasebe kaydı oluşamaz — bu handler zincirin başlangıcıdır.
/// </summary>
public interface IOrderCompletedInvoiceHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, CancellationToken ct);
}

public sealed class OrderCompletedInvoiceHandler : IOrderCompletedInvoiceHandler
{
    private readonly IOrderRepository _orderRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderCompletedInvoiceHandler> _logger;

    public OrderCompletedInvoiceHandler(
        IOrderRepository orderRepo,
        IInvoiceRepository invoiceRepo,
        IUnitOfWork unitOfWork,
        ILogger<OrderCompletedInvoiceHandler> logger)
    {
        _orderRepo = orderRepo;
        _invoiceRepo = invoiceRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, CancellationToken ct)
    {
        // Idempotency: aynı sipariş için zaten fatura varsa atla
        var existing = await _invoiceRepo.GetByOrderIdAsync(orderId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogDebug(
                "Sipariş {OrderNumber} için fatura zaten mevcut — InvoiceId={InvoiceId}, atlanıyor.",
                orderNumber, existing.Id);
            return;
        }

        var order = await _orderRepo.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogError("Sipariş bulunamadı — OrderId={OrderId}", orderId);
            return;
        }

        // E-arşiv fatura numarası: MES-YYYYMMDD-XXXXX
        var invoiceNumber = $"MES-{DateTime.UtcNow:yyyyMMdd}-{orderId.ToString("N")[..5].ToUpperInvariant()}";

        _logger.LogInformation(
            "Otomatik fatura oluşturuluyor — Order={OrderNumber}, InvoiceNo={InvoiceNumber}, Tutar={Total}",
            orderNumber, invoiceNumber, totalAmount);

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, invoiceNumber);

        await _invoiceRepo.AddAsync(invoice, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Fatura oluşturuldu — InvoiceId={InvoiceId}, InvoiceNo={InvoiceNumber}, Order={OrderNumber}",
            invoice.Id, invoiceNumber, orderNumber);
    }
}
