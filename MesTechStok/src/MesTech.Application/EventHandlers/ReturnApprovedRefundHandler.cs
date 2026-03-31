using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Zincir 5c: İade onaylandığında ödeme iadesi tetikler.
/// ReturnApprovedEvent → PaymentTransaction.MarkRefunded() + IPaymentProvider.RefundAsync().
/// </summary>
public interface IReturnApprovedRefundHandler
{
    Task HandleAsync(
        Guid returnRequestId, Guid orderId, Guid tenantId,
        decimal refundAmount,
        CancellationToken ct);
}

public sealed class ReturnApprovedRefundHandler : IReturnApprovedRefundHandler
{
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReturnApprovedRefundHandler> _logger;

    public ReturnApprovedRefundHandler(
        IOrderRepository orderRepo,
        IUnitOfWork uow,
        ILogger<ReturnApprovedRefundHandler> logger)
    {
        _orderRepo = orderRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid returnRequestId, Guid orderId, Guid tenantId,
        decimal refundAmount,
        CancellationToken ct)
    {
        if (refundAmount <= 0)
        {
            _logger.LogWarning(
                "İade tutarı sıfır veya negatif — refund atlandı. ReturnId={ReturnId}, Amount={Amount}",
                returnRequestId, refundAmount);
            return;
        }

        // Sipariş üzerinden ödeme bilgisine erişim
        var order = await _orderRepo.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogError(
                "İade refund — sipariş bulunamadı: OrderId={OrderId}, ReturnId={ReturnId}",
                orderId, returnRequestId);
            return;
        }

        _logger.LogInformation(
            "İade refund işleniyor — ReturnId={ReturnId}, OrderId={OrderId}, Amount={Amount:C2}",
            returnRequestId, orderId, refundAmount);

        // PaymentTransaction güncellemesi yapılmıyor çünkü IPaymentTransactionRepository yok.
        // Gerçek ödeme iadesi IPaymentProvider.RefundAsync() üzerinden yapılır.
        // Bu handler şimdilik refund talebini loglar ve order'ı günceller.
        // IPaymentProvider entegrasyonu DEV3 scope — adapter implemente edince wire edilecek.

        order.Notes = string.IsNullOrEmpty(order.Notes)
            ? $"REFUND: {refundAmount:N2} TRY — ReturnId: {returnRequestId}"
            : $"{order.Notes}\nREFUND: {refundAmount:N2} TRY — ReturnId: {returnRequestId}";

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "İade refund kaydı tamamlandı — ReturnId={ReturnId}, OrderId={OrderId}, RefundAmount={Amount:C2}",
            returnRequestId, orderId, refundAmount);
    }
}
