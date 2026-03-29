using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockPlacements;

public sealed class GetStockPlacementsHandler
    : IRequestHandler<GetStockPlacementsQuery, IReadOnlyList<StockPlacementDto>>
{
    private readonly IStockPlacementRepository _repo;

    public GetStockPlacementsHandler(IStockPlacementRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<StockPlacementDto>> Handle(
        GetStockPlacementsQuery request, CancellationToken cancellationToken)
    {
        var placements = await _repo.GetByTenantAsync(
            request.TenantId, request.WarehouseId, request.ShelfCode, cancellationToken);

        return placements
            .OrderBy(p => p.WarehouseName)
            .ThenBy(p => p.ShelfCode)
            .ThenBy(p => p.ProductName)
            .Select(p => new StockPlacementDto
            {
                Id = p.Id,
                ProductName = p.ProductName ?? "—",
                ProductSku = p.ProductSku,
                Quantity = p.Quantity,
                MinimumStock = p.MinimumStock,
                WarehouseName = p.WarehouseName,
                ShelfCode = p.ShelfCode,
                BinCode = p.BinCode,
                StockStatus = p.StockStatus
            })
            .ToList()
            .AsReadOnly();
    }
}
