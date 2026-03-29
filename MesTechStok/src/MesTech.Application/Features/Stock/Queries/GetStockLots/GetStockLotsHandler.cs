using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockLots;

public sealed class GetStockLotsHandler
    : IRequestHandler<GetStockLotsQuery, IReadOnlyList<StockLotDto>>
{
    private readonly IStockLotRepository _repo;

    public GetStockLotsHandler(IStockLotRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<StockLotDto>> Handle(
        GetStockLotsQuery request, CancellationToken cancellationToken)
    {
        var lots = await _repo.GetByTenantAsync(request.TenantId, request.Limit, cancellationToken);

        return lots
            .OrderByDescending(l => l.ReceivedAt)
            .Select(l => new StockLotDto
            {
                Id = l.Id,
                LotNumber = l.LotNumber,
                ProductName = l.Product?.Name ?? "—",
                ProductSku = l.Product?.SKU,
                Quantity = l.Quantity,
                RemainingQuantity = l.RemainingQuantity,
                UnitCost = l.UnitCost,
                SupplierName = l.SupplierName,
                WarehouseName = l.WarehouseName,
                ExpiryDate = l.ExpiryDate,
                ReceivedAt = l.ReceivedAt,
                IsExpired = l.IsExpired,
                IsFullyConsumed = l.IsFullyConsumed
            })
            .ToList()
            .AsReadOnly();
    }
}
