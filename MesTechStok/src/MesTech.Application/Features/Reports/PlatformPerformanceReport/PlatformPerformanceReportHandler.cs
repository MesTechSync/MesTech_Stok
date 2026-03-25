using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.PlatformPerformanceReport;

public sealed class PlatformPerformanceReportHandler
    : IRequestHandler<PlatformPerformanceReportQuery, PlatformPerformanceReportDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICommissionRecordRepository _commissionRepo;

    public PlatformPerformanceReportHandler(
        IOrderRepository orderRepo,
        ICommissionRecordRepository commissionRepo)
    {
        _orderRepo = orderRepo;
        _commissionRepo = commissionRepo;
    }

    public async Task<PlatformPerformanceReportDto> Handle(
        PlatformPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepo.GetByDateRangeWithItemsAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        // Platform bazlı gruplama
        var byPlatform = orders
            .Where(o => o.SourcePlatform.HasValue)
            .GroupBy(o => o.SourcePlatform!.Value)
            .ToList();

        var metrics = new List<PlatformMetricsDto>();

        foreach (var group in byPlatform)
        {
            var platformType = group.Key;
            var platformOrders = group.ToList();
            var orderCount = platformOrders.Count;
            var revenue = platformOrders.Sum(o => o.TotalAmount);
            var returnCount = platformOrders.Count(o => o.Status == OrderStatus.Cancelled);
            var returnRate = orderCount > 0 ? Math.Round((decimal)returnCount / orderCount * 100, 2) : 0;
            var avgOrderValue = orderCount > 0 ? Math.Round(revenue / orderCount, 2) : 0;

            // Komisyon hesabı
            var commRecords = await _commissionRepo.GetByPlatformAsync(
                request.TenantId, platformType.ToString(),
                request.StartDate, request.EndDate, cancellationToken);
            var commissionPaid = commRecords.Sum(r => r.CommissionAmount);
            var netRevenue = revenue - commissionPaid;

            // Performans skoru: order volume (30%) + revenue (30%) + low return rate (20%) + AOV (20%)
            // Normalize 0-100
            var score = CalculateScore(orderCount, revenue, returnRate, avgOrderValue);

            metrics.Add(new PlatformMetricsDto(
                platformType, platformType.ToString(), orderCount, revenue,
                returnCount, returnRate, avgOrderValue, commissionPaid, netRevenue, score));
        }

        var sorted = metrics.OrderByDescending(m => m.NetRevenue).ToList();

        return new PlatformPerformanceReportDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Platforms = sorted,
            BestPlatform = sorted.FirstOrDefault()?.PlatformType,
            WorstPlatform = sorted.LastOrDefault()?.PlatformType
        };
    }

    private static decimal CalculateScore(int orderCount, decimal revenue, decimal returnRate, decimal avgOrderValue)
    {
        // Simple scoring: each component 0-25 points, total 0-100
        var orderScore = Math.Min(orderCount / 10.0m, 25); // 250 orders = max
        var revenueScore = Math.Min(revenue / 4000, 25); // 100K = max
        var returnScore = Math.Max(0, 25 - returnRate); // 0% return = max
        var aovScore = Math.Min(avgOrderValue / 20, 25); // 500 TL AOV = max

        return Math.Round(orderScore + revenueScore + returnScore + aovScore, 1);
    }
}
