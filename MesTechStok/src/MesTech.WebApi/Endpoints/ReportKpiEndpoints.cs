using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetLowStockProducts;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Sprint 2 — Dalga 14: KPI Dashboard + Rapor Aggregate Endpoint'leri.
/// Mevcut dashboard widget query'lerini compose ederek tek endpoint'te sunar.
/// </summary>
public static class ReportKpiEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reports/dashboard")
            .WithTags("Reports KPI")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/reports/dashboard/kpis — Güncel KPI'lar (S2-DEV6)
        group.MapGet("/kpis", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            // Paralel compose — 4 query aynı anda
            var salesTodayTask = mediator.Send(new GetSalesTodayQuery(tenantId), ct);
            var pendingOrdersTask = mediator.Send(new GetOrdersPendingQuery(tenantId), ct);
            var stockAlertsTask = mediator.Send(new GetStockAlertsQuery(tenantId), ct);
            var platformHealthTask = mediator.Send(new GetPlatformHealthQuery(tenantId), ct);

            await Task.WhenAll(salesTodayTask, pendingOrdersTask, stockAlertsTask, platformHealthTask);

            return Results.Ok(new
            {
                timestamp = DateTime.UtcNow,
                salesToday = salesTodayTask.Result,
                pendingOrders = pendingOrdersTask.Result,
                stockAlerts = stockAlertsTask.Result,
                platformHealth = platformHealthTask.Result
            });
        })
        .WithName("GetDashboardKpis")
        .WithSummary("KPI dashboard — satış, sipariş, stok, platform sağlığı (paralel compose)")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/reports/dashboard/summary — Tam dashboard özeti (S2-DEV6)
        group.MapGet("/summary", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDashboardSummaryQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDashboardSummaryReport")
        .WithSummary("Dashboard tam özeti — ürün, sipariş, stok, gelir metrikleri")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/reports/dashboard/top-products — En çok satan ürünler (S2-DEV6)
        group.MapGet("/top-products", async (
            Guid tenantId,
            int? limit,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTopProductsQuery(tenantId, Math.Clamp(limit ?? 10, 1, 50)), ct);
            return Results.Ok(result);
        })
        .WithName("GetTopProductsReport")
        .WithSummary("En çok satan ürünler — satış adetine göre sıralı")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/dashboard/low-stock — Düşük stok raporu (S2-DEV6)
        group.MapGet("/low-stock", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLowStockProductsQuery(), ct);
            return Results.Ok(new { total = result.Count, products = result });
        })
        .WithName("GetLowStockReport")
        .WithSummary("Düşük stok raporu — minimum stok altındaki ürünler")
        .Produces(200)
        .CacheOutput("Dashboard30s");
    }
}
