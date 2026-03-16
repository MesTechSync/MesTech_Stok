using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.StockTurnoverReport;

/// <summary>
/// Stok devir hizi raporu handler'i.
/// StockMovement + Product verilerini urun bazinda gruplayarak devir metrikleri hesaplar.
/// </summary>
public class StockTurnoverReportHandler
    : IRequestHandler<StockTurnoverReportQuery, IReadOnlyList<StockTurnoverReportDto>>
{
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;

    public StockTurnoverReportHandler(
        IStockMovementRepository movementRepository,
        IProductRepository productRepository)
    {
        _movementRepository = movementRepository;
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<StockTurnoverReportDto>> Handle(
        StockTurnoverReportQuery request, CancellationToken cancellationToken)
    {
        var movements = await _movementRepository.GetByDateRangeAsync(request.StartDate, request.EndDate);
        var products = await _productRepository.GetAllAsync();

        // Apply category filter if specified
        if (request.CategoryFilter.HasValue)
        {
            products = products
                .Where(p => p.CategoryId == request.CategoryFilter.Value)
                .ToList()
                .AsReadOnly();
        }

        var productMap = products.ToDictionary(p => p.Id);
        var periodDays = Math.Max((request.EndDate - request.StartDate).TotalDays, 1);

        // Group negative (outbound/sale) movements by product
        var salesByProduct = movements
            .Where(m => m.Quantity < 0) // Negative = outbound (sales, shipments)
            .GroupBy(m => m.ProductId)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(m => m.Quantity)));

        var result = new List<StockTurnoverReportDto>();

        foreach (var product in products)
        {
            var soldQuantity = salesByProduct.TryGetValue(product.Id, out var sold) ? sold : 0;

            if (soldQuantity == 0)
                continue;

            // Average stock: current stock + sold/2 (simple approximation)
            var avgStock = Math.Max(product.Stock + (soldQuantity / 2.0), 1);

            // Turnover rate: sold quantity / average stock (annualized)
            var turnoverRate = (soldQuantity / avgStock) * (365.0 / periodDays);

            // Average stock days: how many days on average an item stays in stock
            var avgStockDays = turnoverRate > 0 ? 365.0 / turnoverRate : 0;

            // Days of supply: at current rate, how many days will current stock last
            var dailySales = soldQuantity / periodDays;
            var daysOfSupply = dailySales > 0 ? product.Stock / dailySales : 0;

            result.Add(new StockTurnoverReportDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                SoldQuantity = soldQuantity,
                AvgStockDays = Math.Round(avgStockDays, 1),
                TurnoverRate = Math.Round(turnoverRate, 2),
                DaysOfSupply = Math.Round(daysOfSupply, 1)
            });
        }

        return result
            .OrderByDescending(r => r.TurnoverRate)
            .ToList()
            .AsReadOnly();
    }
}
