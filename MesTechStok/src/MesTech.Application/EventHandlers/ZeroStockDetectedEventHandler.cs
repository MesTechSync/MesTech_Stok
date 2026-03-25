using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Stok sıfıra düştüğünde ürünü pasife alır ve platformlara bildirir.
/// Zincir 8: ZeroStockDetected → Deactivate → Platform sync.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IZeroStockEventHandler
{
    Task HandleAsync(Guid productId, string sku, Guid tenantId, CancellationToken ct);
}

public sealed class ZeroStockDetectedEventHandler : IZeroStockEventHandler
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ZeroStockDetectedEventHandler> _logger;

    public ZeroStockDetectedEventHandler(
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ILogger<ZeroStockDetectedEventHandler> logger)
    {
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(Guid productId, string sku, Guid tenantId, CancellationToken ct)
    {
        _logger.LogCritical(
            "ZeroStockDetected → ürün pasife alınıyor. ProductId={ProductId}, SKU={SKU}, TenantId={TenantId}",
            productId, sku, tenantId);

        var product = await _productRepo.GetByIdAsync(productId);
        if (product is null)
        {
            _logger.LogError(
                "Product {ProductId} bulunamadı — SKU={SKU}, deactivation atlandı",
                productId, sku);
            return;
        }

        if (!product.IsActive)
        {
            _logger.LogInformation(
                "Product {SKU} zaten pasif — tekrar deactivate atlandı", sku);
            return;
        }

        product.Deactivate();

        _logger.LogWarning(
            "Product {SKU} deactivated due to zero stock. ProductId={ProductId}, TenantId={TenantId}",
            sku, productId, tenantId);

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
