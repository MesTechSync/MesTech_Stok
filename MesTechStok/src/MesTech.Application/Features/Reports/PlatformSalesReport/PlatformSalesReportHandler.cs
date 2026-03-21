#pragma warning disable MA0051 // Method is too long — report handler is a single cohesive operation
#pragma warning disable NX0003 // NullForgiving justified — null filtered by Where/HasValue clause
using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.PlatformSalesReport;

/// <summary>
/// Platform satis raporu handler'i.
/// Orders + CommissionRecords + SettlementBatches verilerini platform bazinda gruplar.
/// </summary>
public class PlatformSalesReportHandler
    : IRequestHandler<PlatformSalesReportQuery, IReadOnlyList<PlatformSalesReportDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICommissionRecordRepository _commissionRepository;
    private readonly ISettlementBatchRepository _settlementRepository;

    public PlatformSalesReportHandler(
        IOrderRepository orderRepository,
        ICommissionRecordRepository commissionRepository,
        ISettlementBatchRepository settlementRepository)
    {
        _orderRepository = orderRepository;
        _commissionRepository = commissionRepository;
        _settlementRepository = settlementRepository;
    }

    public async Task<IReadOnlyList<PlatformSalesReportDto>> Handle(
        PlatformSalesReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        var settlements = await _settlementRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        // Platform names from the PlatformType enum
        var platformNames = Enum.GetValues<PlatformType>()
            .Select(p => p.ToString())
            .ToList();

        // Group orders by platform
        var ordersByPlatform = orders
            .Where(o => o.SourcePlatform.HasValue)
            .GroupBy(o => o.SourcePlatform!.Value.ToString(), StringComparer.Ordinal) // NX0003: Null filtered by Where clause above
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        // Group settlements (commission data) by platform
        var commissionByPlatform = settlements
            .GroupBy(s => s.Platform, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalCommission), StringComparer.Ordinal);

        var result = new List<PlatformSalesReportDto>();

        foreach (var platform in platformNames)
        {
            // Apply filter if specified
            if (!string.IsNullOrWhiteSpace(request.PlatformFilter) &&
                !platform.Equals(request.PlatformFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ordersByPlatform.TryGetValue(platform, out var platformOrders))
                continue;

            var totalOrders = platformOrders.Count;
            var totalRevenue = platformOrders.Sum(o => o.TotalAmount);

            // Count returns: orders with Cancelled/Returned status
            var returns = platformOrders.Count(o =>
                o.Status == OrderStatus.Cancelled);

            var commissions = commissionByPlatform.TryGetValue(platform, out var c) ? c : 0m;

            // Fallback: use order-level commission if no settlement data
            if (commissions == 0m)
            {
                commissions = platformOrders
                    .Where(o => o.CommissionAmount.HasValue)
                    .Sum(o => o.CommissionAmount!.Value); // NX0003: Null filtered by Where clause above
            }

            var netRevenue = totalRevenue - commissions;

            result.Add(new PlatformSalesReportDto
            {
                Platform = platform,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                Returns = returns,
                Commissions = commissions,
                NetRevenue = netRevenue
            });
        }

        return result
            .OrderByDescending(r => r.TotalRevenue)
            .ToList()
            .AsReadOnly();
    }
}
