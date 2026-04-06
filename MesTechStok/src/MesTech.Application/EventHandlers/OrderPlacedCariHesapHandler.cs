using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Siparis onaylandiginda musteri (CariHesap) kaydini kontrol eder,
/// yoksa otomatik olusturur. Idempotent — ayni isimli CariHesap varsa atlar.
/// Tetikleyici: OrderPlacedEvent (Order.Place() icinden firlatirilir)
/// </summary>
public interface IOrderPlacedCariHesapHandler
{
    Task HandleAsync(Guid orderId, Guid tenantId, CancellationToken ct);
}

public sealed class OrderPlacedCariHesapHandler : IOrderPlacedCariHesapHandler
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICariHesapRepository _cariHesapRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderPlacedCariHesapHandler> _logger;

    public OrderPlacedCariHesapHandler(
        IOrderRepository orderRepo,
        ICariHesapRepository cariHesapRepo,
        IUnitOfWork unitOfWork,
        ILogger<OrderPlacedCariHesapHandler> logger)
    {
        _orderRepo = orderRepo;
        _cariHesapRepo = cariHesapRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(Guid orderId, Guid tenantId, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogDebug("OrderPlacedCariHesap: Order {OrderId} bulunamadi", orderId);
            return;
        }

        if (string.IsNullOrWhiteSpace(order.CustomerName))
        {
            _logger.LogDebug("OrderPlacedCariHesap: Order {OrderNumber} musteri adi bos — atlaniyor", order.OrderNumber);
            return;
        }

        // Idempotent — ayni isimli CariHesap varsa olusturma
        var existing = await _cariHesapRepo.GetByNameAsync(tenantId, order.CustomerName, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogDebug("CariHesap zaten mevcut: {Name} → {CariId}", order.CustomerName, existing.Id);
            return;
        }

        var cariHesap = new CariHesap
        {
            TenantId = tenantId,
            Name = order.CustomerName,
            Email = order.CustomerEmail,
            Phone = order.RecipientPhone,
            Address = order.ShippingAddress,
            Type = CariHesapType.Musteri
        };

        await _cariHesapRepo.AddAsync(cariHesap, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "CariHesap otomatik olusturuldu: {Name} — Order={OrderNumber}, CariId={CariId}",
            order.CustomerName, order.OrderNumber, cariHesap.Id);
    }
}
