using MediatR;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;

namespace MesTech.Application.Queries.GetInventoryValue;

public sealed class GetInventoryValueHandler : IRequestHandler<GetInventoryValueQuery, InventoryValueResult>
{
    private readonly IProductRepository _productRepository;
    private readonly StockCalculationService _stockCalc;

    public GetInventoryValueHandler(IProductRepository productRepository, StockCalculationService stockCalc)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _stockCalc = stockCalc ?? throw new ArgumentNullException(nameof(stockCalc));
    }

    public async Task<InventoryValueResult> Handle(GetInventoryValueQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var products = await _productRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return new InventoryValueResult
        {
            TotalValue = _stockCalc.CalculateInventoryValue(products),
            TotalProducts = products.Count,
            TotalStock = products.Sum(p => p.Stock),
            LowStockCount = products.Count(p => p.IsLowStock()),
            OutOfStockCount = products.Count(p => p.IsOutOfStock())
        };
    }
}
