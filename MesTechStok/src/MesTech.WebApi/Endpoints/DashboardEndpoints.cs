using System.Globalization;
using MediatR;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Application.Queries.ListOrders;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class DashboardEndpoints
{
    private static readonly Dictionary<PlatformType, string> PlatformColors = new()
    {
        [PlatformType.Trendyol] = "#FF6600",
        [PlatformType.Hepsiburada] = "#ff6000",
        [PlatformType.Ciceksepeti] = "#6c3f99",
        [PlatformType.N11] = "#0DB866",
        [PlatformType.Pazarama] = "#E91E63",
        [PlatformType.OpenCart] = "#2c3e50",
        [PlatformType.Amazon] = "#FF9900",
        [PlatformType.Bitrix24] = "#2FC6F6"
    };

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dashboard").WithTags("Dashboard").RequireRateLimiting("PerApiKey");

        // GET /api/v1/dashboard/kpi — KPI summary (products, orders, inventory value, low stock alerts)
        group.MapGet("/kpi", async (ISender mediator, CancellationToken ct) =>
        {
            var productStatus = await mediator.Send(new GetProductDbStatusQuery(), ct);
            var inventoryStats = await mediator.Send(new GetInventoryStatisticsQuery(), ct);
            var recentOrders = await mediator.Send(
                new ListOrdersQuery(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null), ct);

            return Results.Ok(new
            {
                totalProducts = productStatus.TotalCount,
                activeOrders = recentOrders.Count,
                totalInventoryValue = inventoryStats.TotalInventoryValue,
                lowStockAlerts = inventoryStats.LowStockCount
            });
        });

        // GET /api/v1/dashboard/sales-trend?days=7 — daily order counts per platform (Chart.js format)
        group.MapGet("/sales-trend", async (int? days, ISender mediator, CancellationToken ct) =>
        {
            var period = Math.Clamp(days ?? 7, 1, 90);
            var from = DateTime.UtcNow.AddDays(-period).Date;
            var to = DateTime.UtcNow;

            var orders = await mediator.Send(new ListOrdersQuery(from, to, null), ct);

            // Group by date + platform
            var grouped = orders
                .Where(o => o.SourcePlatform is not null)
                .GroupBy(o => new { Date = o.OrderDate.Date, Platform = o.SourcePlatform!.Value })
                .ToDictionary(g => g.Key, g => g.Count());

            // Build date labels
            var labels = Enumerable.Range(0, period)
                .Select(i => from.AddDays(i))
                .ToList();

            var labelStrings = labels
                .Select(d => d.ToString("dd/MM", CultureInfo.InvariantCulture))
                .ToArray();

            // Build per-platform datasets
            var activePlatforms = orders
                .Where(o => o.SourcePlatform is not null)
                .Select(o => o.SourcePlatform!.Value)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var datasets = activePlatforms.Select(platform => new
            {
                label = platform.ToString(),
                data = labels.Select(d =>
                    grouped.TryGetValue(new { Date = d, Platform = platform }, out var count) ? count : 0
                ).ToArray(),
                color = PlatformColors.GetValueOrDefault(platform, "#888888")
            }).ToArray();

            return Results.Ok(new { labels = labelStrings, datasets });
        });

        // GET /api/v1/dashboard/inventory-stats — full inventory statistics
        group.MapGet("/inventory-stats", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryStatisticsQuery(), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/dashboard/recent-orders — orders from last 7 days
        group.MapGet("/recent-orders", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListOrdersQuery(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null), ct);
            return Results.Ok(result);
        });
    }
}
