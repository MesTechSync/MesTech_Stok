using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.CustomerLifetimeValueReport;

/// <summary>
/// Musteri yasam boyu degeri raporu handler'i.
/// Siparisleri musteri bazinda gruplayarak CLV metrikleri hesaplar.
/// CLV = AverageOrderValue * EstimatedAnnualOrders (basit model).
/// </summary>
public class CustomerLifetimeValueReportHandler
    : IRequestHandler<CustomerLifetimeValueReportQuery, IReadOnlyList<CustomerLifetimeValueReportDto>>
{
    private readonly IOrderRepository _orderRepository;

    public CustomerLifetimeValueReportHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<IReadOnlyList<CustomerLifetimeValueReportDto>> Handle(
        CustomerLifetimeValueReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        var customerGroups = orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() >= request.MinOrderCount);

        var result = new List<CustomerLifetimeValueReportDto>();

        foreach (var group in customerGroups)
        {
            var customerOrders = group.OrderBy(o => o.OrderDate).ToList();
            var totalSpent = customerOrders.Sum(o => o.TotalAmount);
            var orderCount = customerOrders.Count;
            var avgOrderValue = totalSpent / orderCount;
            var firstOrder = customerOrders.First().OrderDate;
            var lastOrder = customerOrders.Last().OrderDate;
            var daysSinceLast = (int)(DateTime.UtcNow - lastOrder).TotalDays;

            // Simple CLV estimation: AOV * estimated annual frequency
            var customerLifespanDays = Math.Max((lastOrder - firstOrder).TotalDays, 1);
            var ordersPerYear = orderCount / (customerLifespanDays / 365.0);
            var estimatedClv = avgOrderValue * (decimal)Math.Min(ordersPerYear, 365); // Cap at daily

            // Use first available name, fallback to customer ID
            var customerName = customerOrders
                .Select(o => o.CustomerName)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                ?? group.Key.ToString()[..8];

            result.Add(new CustomerLifetimeValueReportDto
            {
                CustomerId = group.Key,
                CustomerName = customerName,
                TotalOrders = orderCount,
                TotalSpent = totalSpent,
                AverageOrderValue = Math.Round(avgOrderValue, 2),
                FirstOrderDate = firstOrder,
                LastOrderDate = lastOrder,
                DaysSinceLastOrder = daysSinceLast,
                EstimatedCLV = Math.Round(estimatedClv, 2)
            });
        }

        return result
            .OrderByDescending(r => r.EstimatedCLV)
            .ToList()
            .AsReadOnly();
    }
}
