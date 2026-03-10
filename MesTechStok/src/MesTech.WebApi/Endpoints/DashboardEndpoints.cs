using MediatR;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Application.Queries.ListOrders;

namespace MesTech.WebApi.Endpoints;

public static class DashboardEndpoints
{
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
