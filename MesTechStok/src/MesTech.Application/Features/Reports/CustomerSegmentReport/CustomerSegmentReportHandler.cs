using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.CustomerSegmentReport;

/// <summary>
/// Musteri segment raporu handler'i.
/// Orders verisini musteri bazinda gruplayarak siparis sikligi ve tutarina gore segmentler.
/// Segmentasyon: VIP (5+ siparis veya 10K+ TRY), Regular (2-4 siparis), New (1 siparis), Dormant (0 siparis donemde).
/// </summary>
public class CustomerSegmentReportHandler
    : IRequestHandler<CustomerSegmentReportQuery, IReadOnlyList<CustomerSegmentReportDto>>
{
    private readonly IOrderRepository _orderRepository;

    // Segment thresholds
    private const int VipOrderThreshold = 5;
    private const decimal VipRevenueThreshold = 10_000m;
    private const int RegularMinOrders = 2;

    public CustomerSegmentReportHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<CustomerSegmentReportDto>> Handle(
        CustomerSegmentReportQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        // Group orders by customer
        var customerGroups = orders
            .Where(o => o.CustomerId != Guid.Empty)
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount),
                AvgOrderValue = g.Average(o => o.TotalAmount)
            })
            .ToList();

        // Classify each customer into a segment
        var segmented = customerGroups.Select(c =>
        {
            string segment;
            if (c.OrderCount >= VipOrderThreshold || c.TotalRevenue >= VipRevenueThreshold)
                segment = "VIP";
            else if (c.OrderCount >= RegularMinOrders)
                segment = "Regular";
            else
                segment = "New";

            return new { c.CustomerId, Segment = segment, c.TotalRevenue, c.AvgOrderValue };
        }).ToList();

        // Aggregate by segment
        var result = segmented
            .GroupBy(s => s.Segment)
            .Select(g => new CustomerSegmentReportDto
            {
                Segment = g.Key,
                CustomerCount = g.Count(),
                AvgOrderValue = Math.Round(g.Average(c => c.AvgOrderValue), 2),
                TotalRevenue = g.Sum(c => c.TotalRevenue)
            })
            .ToList();

        // Add Dormant segment placeholder (customers with no orders in this period
        // would require a full customer list — excluded here to avoid heavy JOIN;
        // can be enriched if ICustomerRepository provides tenant-scoped GetAll).

        // Sort: VIP > Regular > New > Dormant
        var segmentOrder = new Dictionary<string, int>
        {
            ["VIP"] = 0,
            ["Regular"] = 1,
            ["New"] = 2,
            ["Dormant"] = 3
        };

        return result
            .OrderBy(r => segmentOrder.TryGetValue(r.Segment, out var order) ? order : 99)
            .ToList()
            .AsReadOnly();
    }
}
