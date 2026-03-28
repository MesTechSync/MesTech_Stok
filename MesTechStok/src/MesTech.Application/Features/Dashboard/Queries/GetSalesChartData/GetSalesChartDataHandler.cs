using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;

/// <summary>
/// Platform bazlı satış grafiği handler.
/// Son N gün, platformları ayrı serilere böler.
/// Sipariş olmayan günler sıfır olarak doldurulur.
/// </summary>
public sealed class GetSalesChartDataHandler
    : IRequestHandler<GetSalesChartDataQuery, SalesChartDataDto>
{
    private readonly IOrderRepository _orderRepo;

    public GetSalesChartDataHandler(IOrderRepository orderRepo)
        => _orderRepo = orderRepo;

    public async Task<SalesChartDataDto> Handle(
        GetSalesChartDataQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var days = Math.Clamp(request.Days, 1, 365);
        var from = DateTime.UtcNow.AddDays(-days).Date;
        var to = DateTime.UtcNow;

        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId, from, to, cancellationToken).ConfigureAwait(false);

        // Platform filtresi
        var filtered = request.PlatformCode is not null
            ? orders.Where(o => string.Equals(
                o.SourcePlatform?.ToString(), request.PlatformCode, StringComparison.OrdinalIgnoreCase))
            : orders;

        // Tarih etiketleri
        var labels = Enumerable.Range(0, days)
            .Select(i => from.AddDays(i).ToString("dd MMM"))
            .ToList();

        // Platform bazlı gruplama
        var platformGroups = filtered
            .Where(o => o.SourcePlatform.HasValue)
            .GroupBy(o => o.SourcePlatform!.Value.ToString())
            .ToList();

        var series = new List<SalesChartSeriesDto>();

        foreach (var pg in platformGroups)
        {
            var dailyData = pg
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => (
                    Revenue: g.Sum(o => o.TotalAmount),
                    Count: g.Count()));

            var revenueValues = new List<decimal>(days);
            var orderCounts = new List<int>(days);

            for (var i = 0; i < days; i++)
            {
                var date = from.AddDays(i);
                var data = dailyData.GetValueOrDefault(date);
                revenueValues.Add(data.Revenue);
                orderCounts.Add(data.Count);
            }

            series.Add(new SalesChartSeriesDto
            {
                PlatformCode = pg.Key,
                PlatformName = pg.Key,
                RevenueValues = revenueValues.AsReadOnly(),
                OrderCountValues = orderCounts.AsReadOnly()
            });
        }

        return new SalesChartDataDto
        {
            Labels = labels.AsReadOnly(),
            Series = series.AsReadOnly(),
            TotalRevenue = filtered.Sum(o => o.TotalAmount),
            TotalOrders = filtered.Count()
        };
    }
}
