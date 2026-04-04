using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Stock.Queries.GetStockValueReport;

public sealed class GetStockValueReportHandler
    : IRequestHandler<GetStockValueReportQuery, StockValueReportResult>
{
    private readonly IProductRepository _productRepo;

    public GetStockValueReportHandler(IProductRepository productRepo)
        => _productRepo = productRepo ?? throw new ArgumentNullException(nameof(productRepo));

    public async Task<StockValueReportResult> Handle(
        GetStockValueReportQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepo.GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        var lines = products
            .Where(p => p.Stock > 0)
            .Select(p => new StockValueLineDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU,
                Stock = p.Stock,
                Price = p.SalePrice,
                CostPrice = p.PurchasePrice,
                TotalValue = p.Stock * p.SalePrice,
                TotalCost = p.Stock * p.PurchasePrice
            })
            .OrderByDescending(l => l.TotalValue)
            .ToList();

        return new StockValueReportResult
        {
            TotalValue = lines.Sum(l => l.TotalValue),
            TotalCostValue = lines.Sum(l => l.TotalCost),
            UnrealizedProfitLoss = lines.Sum(l => l.TotalValue - l.TotalCost),
            TotalProducts = products.Count,
            ZeroStockProducts = products.Count(p => p.Stock == 0),
            TopValueProducts = lines.Take(20).ToList()
        };
    }
}
