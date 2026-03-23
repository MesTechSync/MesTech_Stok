using MesTech.Domain.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş onaylandığında gelir kaydı oluşturur (Zincir 2).
/// OrderPlacedEvent tetikler → Income entity oluşturulur.
/// </summary>
public interface IOrderConfirmedRevenueHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, Guid? storeId,
        CancellationToken ct);
}

public class OrderConfirmedRevenueHandler : IOrderConfirmedRevenueHandler
{
    private readonly IIncomeRepository _incomeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderConfirmedRevenueHandler> _logger;

    public OrderConfirmedRevenueHandler(
        IIncomeRepository incomeRepo,
        IUnitOfWork uow,
        ILogger<OrderConfirmedRevenueHandler> logger)
    {
        _incomeRepo = incomeRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, Guid? storeId,
        CancellationToken ct)
    {
        var income = new Income
        {
            TenantId = tenantId,
            StoreId = storeId,
            Description = $"Sipariş geliri — #{orderNumber}",
            IncomeType = Domain.Enums.IncomeType.Satis,
            Source = Domain.Enums.IncomeSource.DirectSale,
            OrderId = orderId,
            Date = DateTime.UtcNow
        };
        income.SetAmount(totalAmount);

        await _incomeRepo.AddAsync(income, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Gelir kaydı oluşturuldu — OrderId={OrderId}, Amount={Amount}",
            orderId, totalAmount);
    }
}
