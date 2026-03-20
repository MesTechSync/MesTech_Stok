#pragma warning disable NX0003 // NullForgiving justified — null filtered by Where/HasValue clause
using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.OrderFulfillmentReport;

/// <summary>
/// Siparis karsilama raporu handler'i.
/// Order verilerini platform bazinda gruplayarak gonderi suresi metrikleri hesaplar.
/// </summary>
public class OrderFulfillmentReportHandler
    : IRequestHandler<OrderFulfillmentReportQuery, IReadOnlyList<OrderFulfillmentReportDto>>
{
    private readonly IOrderRepository _orderRepository;

    public OrderFulfillmentReportHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<IReadOnlyList<OrderFulfillmentReportDto>> Handle(
        OrderFulfillmentReportQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        var platformGroups = orders
            .Where(o => o.SourcePlatform.HasValue)
            .GroupBy(o => o.SourcePlatform!.Value.ToString()); // NX0003: Null filtered by Where clause above

        var result = new List<OrderFulfillmentReportDto>();

        foreach (var group in platformGroups)
        {
            var platformOrders = group.ToList();
            var totalOrders = platformOrders.Count;

            var shippedOrders = platformOrders
                .Where(o => o.ShippedAt.HasValue)
                .ToList();

            var deliveredOrders = platformOrders
                .Where(o => o.DeliveredAt.HasValue)
                .ToList();

            // Average time from order to ship (hours)
            var avgOrderToShip = shippedOrders.Count > 0
                ? shippedOrders.Average(o => (o.ShippedAt!.Value - o.OrderDate).TotalHours) // NX0003
                : 0.0;

            // Average time from ship to deliver (days)
            var shippedAndDelivered = platformOrders
                .Where(o => o.ShippedAt.HasValue && o.DeliveredAt.HasValue)
                .ToList();

            var avgShipToDeliver = shippedAndDelivered.Count > 0
                ? shippedAndDelivered.Average(o => (o.DeliveredAt!.Value - o.ShippedAt!.Value).TotalDays) // NX0003
                : 0.0;

            // Average total fulfillment time (order to deliver, days)
            var avgTotalFulfillment = deliveredOrders.Count > 0
                ? deliveredOrders.Average(o => (o.DeliveredAt!.Value - o.OrderDate).TotalDays) // NX0003
                : 0.0;

            // Fulfillment rate: delivered / total
            var fulfillmentRate = totalOrders > 0
                ? (double)deliveredOrders.Count / totalOrders * 100.0
                : 0.0;

            result.Add(new OrderFulfillmentReportDto
            {
                Platform = group.Key,
                TotalOrders = totalOrders,
                ShippedOrders = shippedOrders.Count,
                DeliveredOrders = deliveredOrders.Count,
                AvgOrderToShipHours = Math.Round(avgOrderToShip, 1),
                AvgShipToDeliverDays = Math.Round(avgShipToDeliver, 1),
                AvgTotalFulfillmentDays = Math.Round(avgTotalFulfillment, 1),
                FulfillmentRate = Math.Round(fulfillmentRate, 1)
            });
        }

        return result
            .OrderByDescending(r => r.TotalOrders)
            .ToList()
            .AsReadOnly();
    }
}
