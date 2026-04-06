using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;

namespace MesTech.WebApi.Endpoints;

public static class DashboardWidgetEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard Widgets")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dashboard/sales-today — bugunku satis ozeti (bugun vs dun)
        group.MapGet("/sales-today", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSalesTodayQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetSalesToday")
        .WithSummary("Bugunku satis ozeti (bugun vs dun karsilastirmali)")
        .Produces<SalesTodayDto>(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/orders-pending — bekleyen siparis sayisi
        group.MapGet("/orders-pending", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetOrdersPendingQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrdersPending")
        .WithSummary("Bekleyen siparis sayisi (Pending + Confirmed)")
        .Produces<OrdersPendingDto>(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/stock-alerts — dusuk stok uyarilari
        group.MapGet("/stock-alerts", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStockAlertsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetWidgetStockAlerts")
        .WithSummary("Dusuk stok uyarilari (stok <= minThreshold)")
        .Produces<IReadOnlyList<StockAlertDto>>(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/platform-health — platform saglik durumu
        group.MapGet("/platform-health", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPlatformHealthQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformHealth")
        .WithSummary("Platform saglik durumu (son sync + 24h hata sayisi)")
        .Produces<IReadOnlyList<PlatformHealthDto>>(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/revenue-chart — gelir grafigi
        group.MapGet("/revenue-chart", async (
            Guid tenantId,
            int? days,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetRevenueChartQuery(tenantId, days ?? 30), ct);
            return Results.Ok(result);
        })
        .WithName("GetRevenueChart")
        .WithSummary("Gelir grafigi (gun bazinda siparis tutari + sayisi)")
        .Produces<IReadOnlyList<RevenueChartPointDto>>(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/top-products — en cok satan urunler
        group.MapGet("/top-products", async (
            Guid tenantId,
            int? limit,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetTopProductsQuery(tenantId, limit ?? 10), ct);
            return Results.Ok(result);
        })
        .WithName("GetTopProducts")
        .WithSummary("En cok satan urunler (gelire gore siralanmis)")
        .Produces<IReadOnlyList<TopProductDto>>(200)
        .CacheOutput("Dashboard30s");
    }
}
