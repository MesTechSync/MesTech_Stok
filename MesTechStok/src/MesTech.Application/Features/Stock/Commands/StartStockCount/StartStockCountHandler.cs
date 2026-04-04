using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Stock.Commands.StartStockCount;

public sealed class StartStockCountHandler : IRequestHandler<StartStockCountCommand, StockCountSessionDto>
{
    private readonly IProductRepository _productRepo;
    private readonly IWarehouseRepository _warehouseRepo;

    public StartStockCountHandler(IProductRepository productRepo, IWarehouseRepository warehouseRepo)
    {
        _productRepo = productRepo;
        _warehouseRepo = warehouseRepo;
    }

    public async Task<StockCountSessionDto> Handle(StartStockCountCommand request, CancellationToken cancellationToken)
    {
        string? warehouseName = null;
        if (request.WarehouseId.HasValue)
        {
            var wh = await _warehouseRepo.GetByIdAsync(request.WarehouseId.Value).ConfigureAwait(false);
            warehouseName = wh?.Name;
        }

        var products = await _productRepo.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var items = products.Select(p => new StockCountItemDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            SKU = p.SKU,
            Barcode = p.Barcode,
            ExpectedStock = p.Stock,
            CountedStock = 0
        }).ToList();

        return new StockCountSessionDto
        {
            SessionId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            WarehouseName = warehouseName,
            Items = items
        };
    }
}
