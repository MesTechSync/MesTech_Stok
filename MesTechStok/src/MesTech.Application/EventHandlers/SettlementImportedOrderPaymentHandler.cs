using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Settlement→Order eslestirme sonucu.
/// </summary>
public sealed record SettlementPaymentResult(int Matched, int NotFound, int AlreadyPaid, int TotalLines);

/// <summary>
/// Settlement import edildiginde ilgili siparislerin odeme durumunu gunceller.
/// Zincir 4a: SettlementImportedEvent → OrderNumber lookup → Order.MarkAsPaid() + SetCommission().
/// Tetikleyici: SettlementImportedEvent (SettlementBatch.Create icinden firlatirilir)
/// </summary>
public interface ISettlementImportedOrderPaymentHandler
{
    Task<SettlementPaymentResult> HandleAsync(Guid settlementBatchId, Guid tenantId, CancellationToken ct);
}

public sealed class SettlementImportedOrderPaymentHandler : ISettlementImportedOrderPaymentHandler
{
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SettlementImportedOrderPaymentHandler> _logger;

    public SettlementImportedOrderPaymentHandler(
        ISettlementBatchRepository settlementRepo,
        IOrderRepository orderRepo,
        IUnitOfWork unitOfWork,
        ILogger<SettlementImportedOrderPaymentHandler> logger)
    {
        _settlementRepo = settlementRepo;
        _orderRepo = orderRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SettlementPaymentResult> HandleAsync(Guid settlementBatchId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation(
            "SettlementImported → siparis odeme guncelleme basliyor. BatchId={BatchId}",
            settlementBatchId);

        var batch = await _settlementRepo.GetByIdAsync(settlementBatchId, ct).ConfigureAwait(false);
        if (batch is null)
        {
            _logger.LogError("SettlementBatch {BatchId} bulunamadi — odeme guncelleme atlanıyor", settlementBatchId);
            return new SettlementPaymentResult(0, 0, 0, 0);
        }

        if (batch.Lines.Count == 0)
        {
            _logger.LogDebug("SettlementBatch {BatchId} bos — eslestirme yapilacak siparis yok", settlementBatchId);
            return new SettlementPaymentResult(0, 0, 0, 0);
        }

        int matched = 0, notFound = 0, alreadyPaid = 0;

        foreach (var line in batch.Lines)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line.OrderId))
            {
                notFound++;
                continue;
            }

            var order = await _orderRepo.GetByOrderNumberAsync(line.OrderId, ct).ConfigureAwait(false);
            if (order is null)
            {
                _logger.LogDebug(
                    "Settlement line OrderId={OrderId} ile eslesen siparis bulunamadi",
                    line.OrderId);
                notFound++;
                continue;
            }

            if (string.Equals(order.PaymentStatus, "Paid", StringComparison.Ordinal))
            {
                alreadyPaid++;
                continue;
            }

            // Komisyon bilgisi set et
            if (line.CommissionAmount != 0m)
            {
                var commissionRate = line.GrossAmount > 0
                    ? (line.CommissionAmount / line.GrossAmount) * 100m
                    : 0m;
                order.SetCommission(commissionRate, line.CommissionAmount);
            }

            // Kargo gideri set et
            if (line.CargoDeduction != 0m)
                order.SetCargoExpense(line.CargoDeduction);

            // Odeme durumunu guncelle
            order.MarkAsPaid();
            await _orderRepo.UpdateAsync(order, ct).ConfigureAwait(false);
            matched++;

            _logger.LogDebug(
                "Siparis {OrderNumber} MarkAsPaid — komisyon={Commission:F2}, kargo={Cargo:F2}, net={Net:F2}",
                order.OrderNumber, line.CommissionAmount, line.CargoDeduction, line.NetAmount);
        }

        if (matched > 0)
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var result = new SettlementPaymentResult(matched, notFound, alreadyPaid, batch.Lines.Count);

        _logger.LogInformation(
            "SettlementImported odeme guncelleme tamamlandi — BatchId={BatchId}: {Matched}/{Total} eslesti, {NotFound} bulunamadi, {AlreadyPaid} zaten odenmis",
            settlementBatchId, matched, batch.Lines.Count, notFound, alreadyPaid);

        return result;
    }
}
