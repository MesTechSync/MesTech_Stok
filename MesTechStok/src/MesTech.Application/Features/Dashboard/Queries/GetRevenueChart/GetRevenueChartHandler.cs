using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;

/// <summary>
/// Gelir grafik isleyicisi.
/// Siparisleri gune gore gruplar, son N gun icin gelir ve siparis sayisi hesaplar.
/// Siparis olmayan gunler sifir olarak doldurulur.
/// </summary>
public sealed class GetRevenueChartHandler : IRequestHandler<GetRevenueChartQuery, IReadOnlyList<RevenueChartPointDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetRevenueChartHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<IReadOnlyList<RevenueChartPointDto>> Handle(
        GetRevenueChartQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var days = Math.Clamp(request.Days, 1, 365);
        var from = DateTime.UtcNow.AddDays(-days).Date;
        var to = DateTime.UtcNow;

        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, from, to, cancellationToken);

        var grouped = orders
            .GroupBy(o => o.OrderDate.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Revenue = g.Sum(o => o.TotalAmount),
                Count = g.Count()
            });

        var result = Enumerable.Range(0, days)
            .Select(i =>
            {
                var date = from.AddDays(i);
                var data = grouped.GetValueOrDefault(date);

                return new RevenueChartPointDto
                {
                    Date = date,
                    Revenue = data?.Revenue ?? 0m,
                    OrderCount = data?.Count ?? 0
                };
            })
            .ToList();

        return result.AsReadOnly();
    }
}
