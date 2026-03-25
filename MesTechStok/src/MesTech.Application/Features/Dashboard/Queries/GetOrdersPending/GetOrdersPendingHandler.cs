using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;

/// <summary>
/// Bekleyen siparis isleyicisi.
/// Pending ve Confirmed durumdaki siparisleri sayar.
/// 24 saatten eski olanlar "urgent" sayilir.
/// En eski siparisin dakika cinsinden yasini hesaplar.
/// </summary>
public sealed class GetOrdersPendingHandler : IRequestHandler<GetOrdersPendingQuery, OrdersPendingDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersPendingHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<OrdersPendingDto> Handle(GetOrdersPendingQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Fetch orders from last 90 days to capture all pending orders
        var from = DateTime.UtcNow.AddDays(-90);
        var to = DateTime.UtcNow;

        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, from, to, cancellationToken);

        var pendingOrders = orders
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)
            .ToList();

        var now = DateTime.UtcNow;
        var urgentThreshold = now.AddHours(-24);

        var urgentCount = pendingOrders.Count(o => o.OrderDate < urgentThreshold);

        var oldestMinutes = pendingOrders.Count > 0
            ? (int)(now - pendingOrders.Min(o => o.OrderDate)).TotalMinutes
            : 0;

        return new OrdersPendingDto
        {
            Count = pendingOrders.Count,
            Urgent = urgentCount,
            OldestMinutes = oldestMinutes
        };
    }
}
