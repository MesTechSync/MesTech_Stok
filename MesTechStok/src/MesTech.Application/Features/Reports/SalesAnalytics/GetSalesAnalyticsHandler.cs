using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Reports.SalesAnalytics;

public sealed class GetSalesAnalyticsHandler : IRequestHandler<GetSalesAnalyticsQuery, SalesAnalyticsDto>
{
    private readonly IOrderRepository _orderRepo;

    public GetSalesAnalyticsHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<SalesAnalyticsDto> Handle(GetSalesAnalyticsQuery request, CancellationToken ct)
    {
        var orders = await _orderRepo.GetByDateRangeWithItemsAsync(request.TenantId, request.From, request.To, ct);

        if (orders.Count == 0)
            return new SalesAnalyticsDto();

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalOrders = orders.Count;
        var avgOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0;
        var totalProductsSold = orders.SelectMany(o => o.OrderItems).Sum(i => i.Quantity);

        // Daily sales
        var dailySales = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                Revenue = Math.Round(g.Sum(o => o.TotalAmount), 2),
                Orders = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Platform breakdown
        var platformGroups = orders
            .GroupBy(o => o.SourcePlatform?.ToString() ?? "Direct")
            .Select(g => new PlatformSalesDto
            {
                Platform = g.Key,
                Revenue = Math.Round(g.Sum(o => o.TotalAmount), 2),
                Orders = g.Count(),
                Percentage = totalRevenue > 0 ? Math.Round(g.Sum(o => o.TotalAmount) / totalRevenue * 100, 2) : 0
            })
            .OrderByDescending(p => p.Revenue)
            .ToList();

        // Top products
        var topProducts = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => new { i.ProductId, i.ProductName, i.ProductSKU })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                SKU = g.Key.ProductSKU,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = Math.Round(g.Sum(i => i.TotalPrice), 2)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(20)
            .ToList();

        return new SalesAnalyticsDto
        {
            TotalRevenue = Math.Round(totalRevenue, 2),
            TotalOrders = totalOrders,
            AverageOrderValue = avgOrderValue,
            TotalProductsSold = totalProductsSold,
            TopSellingPlatform = platformGroups.FirstOrDefault()?.Platform ?? "N/A",
            DailySales = dailySales,
            PlatformBreakdown = platformGroups,
            TopProducts = topProducts
        };
    }
}
