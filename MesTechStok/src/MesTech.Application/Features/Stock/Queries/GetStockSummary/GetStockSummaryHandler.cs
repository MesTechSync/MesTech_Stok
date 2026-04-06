using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Stock.Queries.GetStockSummary;

public sealed class GetStockSummaryHandler : IRequestHandler<GetStockSummaryQuery, StockSummaryResult>
{
    private readonly IProductRepository _productRepo;

    public GetStockSummaryHandler(IProductRepository productRepo)
        => _productRepo = productRepo ?? throw new ArgumentNullException(nameof(productRepo));

    public async Task<StockSummaryResult> Handle(GetStockSummaryQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepo.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return new StockSummaryResult
        {
            TotalProducts = products.Count,
            InStockProducts = products.Count(p => p.Stock > 0),
            OutOfStockProducts = products.Count(p => p.Stock == 0),
            LowStockProducts = products.Count(p => p.Stock > 0 && p.Stock <= p.MinimumStock),
            TotalStockValue = products.Sum(p => p.Stock * p.SalePrice),
            TotalUnits = products.Sum(p => p.Stock)
        };
    }
}
