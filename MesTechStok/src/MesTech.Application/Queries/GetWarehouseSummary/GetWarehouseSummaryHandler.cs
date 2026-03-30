using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetWarehouseSummary;

public sealed class GetWarehouseSummaryHandler : IRequestHandler<GetWarehouseSummaryQuery, IReadOnlyList<WarehouseSummaryDto>>
{
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IProductRepository _productRepo;

    public GetWarehouseSummaryHandler(
        IWarehouseRepository warehouseRepo,
        IProductRepository productRepo)
    {
        _warehouseRepo = warehouseRepo;
        _productRepo = productRepo;
    }

    public async Task<IReadOnlyList<WarehouseSummaryDto>> Handle(
        GetWarehouseSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warehouses = await _warehouseRepo.GetAllAsync();
        if (warehouses.Count == 0)
            return Array.Empty<WarehouseSummaryDto>();

        var tenantWarehouses = warehouses
            .Where(w => w.TenantId == request.TenantId)
            .ToList();

        if (tenantWarehouses.Count == 0)
            return Array.Empty<WarehouseSummaryDto>();

        // Batch fetch all products once, group by warehouse — eliminates N+1 query
        var warehouseIds = tenantWarehouses.Select(w => w.Id).ToHashSet();
        var allProducts = await _productRepo.GetAllAsync();
        var productsByWarehouse = allProducts
            .Where(p => p.WarehouseId.HasValue && warehouseIds.Contains(p.WarehouseId.Value))
            .GroupBy(p => p.WarehouseId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<WarehouseSummaryDto>();

        foreach (var wh in tenantWarehouses)
        {
            var products = productsByWarehouse.GetValueOrDefault(wh.Id) ?? [];

            var outOfStock = products.Count(p => p.Stock == 0);
            var critical = products.Count(p => p.Stock > 0 && p.Stock <= p.MinimumStock);
            var low = products.Count(p => p.Stock > p.MinimumStock && p.Stock <= p.MinimumStock * 2);
            var normal = products.Count(p => p.Stock > p.MinimumStock * 2);

            var capacityPercent = wh.MaxCapacity > 0
                ? (int)Math.Round((decimal)products.Count / wh.MaxCapacity.Value * 100)
                : 0;

            var health = outOfStock > 0 ? "Critical"
                : critical > 0 ? "Warning"
                : "Healthy";

            result.Add(new WarehouseSummaryDto
            {
                WarehouseId = wh.Id,
                Name = wh.Name,
                Location = wh.City,
                ProductCount = products.Count,
                TotalStock = products.Sum(p => p.Stock),
                OutOfStockCount = outOfStock,
                CriticalStockCount = critical,
                LowStockCount = low,
                NormalStockCount = normal,
                MaxCapacity = wh.MaxCapacity,
                CapacityPercent = capacityPercent,
                HealthStatus = health
            });
        }

        return result;
    }
}
