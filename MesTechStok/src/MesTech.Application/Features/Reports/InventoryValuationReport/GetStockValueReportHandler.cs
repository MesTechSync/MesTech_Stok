using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.InventoryValuationReport;

/// <summary>
/// Handler for GetStockValueReportQuery — returns summary + detail stock valuation.
/// Used by StockValueReportViewModel (Avalonia) and WebApi endpoint.
/// </summary>
public sealed class GetStockValueReportHandler
    : IRequestHandler<GetStockValueReportQuery, StockValueReportResult>
{
    private readonly IProductRepository _productRepository;

    public GetStockValueReportHandler(IProductRepository productRepository)
        => _productRepository = productRepository;

    public async Task<StockValueReportResult> Handle(
        GetStockValueReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var products = request.CategoryFilter.HasValue
            ? await _productRepository.GetByCategoryAsync(request.CategoryFilter.Value)
            : await _productRepository.GetAllAsync().ConfigureAwait(false);

        var items = products
            .Where(p => p.Stock > 0)
            .Select(p =>
            {
                var totalCost = p.Stock * p.PurchasePrice;
                var totalSale = p.Stock * p.SalePrice;

                return new InventoryValuationReportDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    SKU = p.SKU,
                    CurrentStock = p.Stock,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    TotalCostValue = totalCost,
                    TotalSaleValue = totalSale,
                    PotentialProfit = totalSale - totalCost
                };
            })
            .OrderByDescending(r => r.TotalCostValue)
            .ToList();

        return new StockValueReportResult
        {
            Items = items.AsReadOnly(),
            TotalCostValue = items.Sum(i => i.TotalCostValue),
            TotalSaleValue = items.Sum(i => i.TotalSaleValue),
            TotalPotentialProfit = items.Sum(i => i.PotentialProfit),
            TotalProducts = items.Count,
            TotalStockUnits = items.Sum(i => i.CurrentStock)
        };
    }
}
