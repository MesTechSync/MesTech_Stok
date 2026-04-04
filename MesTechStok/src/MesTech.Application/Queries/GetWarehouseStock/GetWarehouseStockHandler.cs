using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetWarehouseStock;

public sealed class GetWarehouseStockHandler : IRequestHandler<GetWarehouseStockQuery, IReadOnlyList<WarehouseStockDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public GetWarehouseStockHandler(
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
    }

    public async Task<IReadOnlyList<WarehouseStockDto>> Handle(GetWarehouseStockQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId).ConfigureAwait(false);
        if (warehouse == null)
            return Array.Empty<WarehouseStockDto>();

        var allProducts = await _productRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return allProducts
            .Where(p => p.WarehouseId == request.WarehouseId && p.TenantId == request.TenantId && p.IsActive)
            .Select(p => new WarehouseStockDto
            {
                ProductId = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Barcode = p.Barcode,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                PurchasePrice = p.PurchasePrice,
                SalePrice = p.SalePrice,
                TotalValue = p.TotalValue,
                IsLowStock = p.IsLowStock(),
                IsCriticalStock = p.IsCriticalStock
            })
            .ToList()
            .AsReadOnly();
    }
}
