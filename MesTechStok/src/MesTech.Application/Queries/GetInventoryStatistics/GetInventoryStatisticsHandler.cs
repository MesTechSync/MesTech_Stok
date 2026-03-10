using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetInventoryStatistics;

public class GetInventoryStatisticsHandler : IRequestHandler<GetInventoryStatisticsQuery, InventoryStatisticsDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public GetInventoryStatisticsHandler(
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _stockMovementRepository = stockMovementRepository ?? throw new ArgumentNullException(nameof(stockMovementRepository));
    }

    public async Task<InventoryStatisticsDto> Handle(GetInventoryStatisticsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allProducts = await _productRepository.GetAllAsync().ConfigureAwait(false);
        var lowStockProducts = await _productRepository.GetLowStockAsync().ConfigureAwait(false);

        var todayStart = DateTime.UtcNow.Date;
        var todayMovements = await _stockMovementRepository
            .GetByDateRangeAsync(todayStart, DateTime.UtcNow)
            .ConfigureAwait(false);

        var totalInventoryValue = allProducts.Sum(p => p.Stock * p.SalePrice);
        var outOfStockCount = allProducts.Count(p => p.Stock == 0);
        var criticalStockCount = allProducts.Count(p => p.Stock > 0 && p.Stock <= 5);
        var lowStockCount = lowStockProducts.Count(p => p.Stock > 5);

        return new InventoryStatisticsDto
        {
            TotalInventoryValue = totalInventoryValue,
            LowStockCount = lowStockCount,
            CriticalStockCount = criticalStockCount,
            OutOfStockCount = outOfStockCount,
            TodayMovements = todayMovements.Count,
            TotalItems = allProducts.Count
        };
    }
}
