using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesToday;

/// <summary>
/// Bugunku ve dunku siparis toplamlarini karsilastirir.
/// Yuzde degisim: ((bugun - dun) / dun) * 100. Dun sifirsa ve bugun > 0 ise %100.
/// </summary>
public sealed class GetSalesTodayHandler : IRequestHandler<GetSalesTodayQuery, SalesTodayDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetSalesTodayHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<SalesTodayDto> Handle(GetSalesTodayQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var yesterdayStart = todayStart.AddDays(-1);

        var todayOrders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, todayStart, todayEnd, cancellationToken);
        var yesterdayOrders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, yesterdayStart, todayStart, cancellationToken);

        var todayTotal = todayOrders.Sum(o => o.TotalAmount);
        var yesterdayTotal = yesterdayOrders.Sum(o => o.TotalAmount);

        var changePercent = yesterdayTotal != 0
            ? ((todayTotal - yesterdayTotal) / yesterdayTotal) * 100
            : todayTotal > 0 ? 100m : 0m;

        return new SalesTodayDto
        {
            Today = todayTotal,
            Yesterday = yesterdayTotal,
            ChangePercent = Math.Round(changePercent, 2)
        };
    }
}
