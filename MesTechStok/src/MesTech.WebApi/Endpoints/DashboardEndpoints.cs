using System.Globalization;
using MesTech.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Application.Queries.ListOrders;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;
using MesTech.Application.Features.Dashboard.Queries.GetAppHubData;
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
        })
        .WithName("GetDashboardKpi")
        .WithSummary("Dashboard KPI özeti (ürün, sipariş, envanter, stok uyarıları)")
        .Produces(200)
        .CacheOutput("Dashboard30s");

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

            var datasets = activePlatforms.Select(platform => new ChartDataset(
                Label: platform.ToString(),
                Data: labels.Select(d =>
                    grouped.TryGetValue(new { Date = d, Platform = platform }, out var count) ? count : 0
                ).ToArray(),
                Color: PlatformColors.GetValueOrDefault(platform, "#888888")
            )).ToList();

            return Results.Ok(new ChartResponse(labelStrings, datasets));
        })
        .WithName("GetSalesTrend")
        .WithSummary("Günlük satış trendi (platform bazlı, Chart.js formatı)")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/inventory-stats — full inventory statistics
        group.MapGet("/inventory-stats", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryStatisticsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryStats")
        .WithSummary("Envanter istatistikleri")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/recent-orders — orders from last 7 days
        group.MapGet("/recent-orders", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListOrdersQuery(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null), ct);
            return Results.Ok(result);
        })
        .WithName("GetRecentOrders")
        .WithSummary("Son 7 gün siparişleri")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/accounting-kpi — muhasebe KPI (gelir, gider, kar, siparis metrikleri)
        group.MapGet("/accounting-kpi", async (
            Guid tenantId, ISender mediator, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var summary = await mediator.Send(
                new GetMonthlySummaryQuery(now.Year, now.Month, tenantId), ct);

            return Results.Ok(new
            {
                month = $"{now.Year}-{now.Month:D2}",
                totalSales = summary.TotalSales,
                totalExpenses = summary.TotalExpenses,
                netProfit = summary.TotalSales - summary.TotalExpenses,
                totalOrders = summary.TotalOrders,
                totalReturns = summary.TotalReturns,
                returnRate = summary.ReturnRate,
                averageOrderValue = summary.AverageOrderValue,
                totalCommissions = summary.TotalCommissions,
                totalShippingCost = summary.TotalShippingCost,
                totalTaxDue = summary.TotalTaxDue
            });
        })
        .WithName("GetAccountingKpi")
        .WithSummary("Muhasebe KPI — aylik gelir, gider, kar, siparis metrikleri")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/low-stock-alerts — products below reorder point
        group.MapGet("/low-stock-alerts", async (
            Guid tenantId,
            int? count,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetLowStockAlertsQuery(tenantId, count ?? 20), ct);
            return Results.Ok(result);
        })
        .WithName("GetLowStockAlerts")
        .WithSummary("Düşük stok uyarıları — yeniden sipariş noktası altı ürünler")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/pending-invoices — invoices awaiting approval/send
        group.MapGet("/pending-invoices", async (
            Guid tenantId,
            int? count,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPendingInvoicesQuery(tenantId, count ?? 10), ct);
            return Results.Ok(result);
        })
        .WithName("GetPendingInvoices")
        .WithSummary("Bekleyen faturalar — onay/gönderim bekleyenler")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/sales-chart — sales chart data (configurable days + platform)
        group.MapGet("/sales-chart", async (
            Guid tenantId,
            int? days,
            string? platformCode,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSalesChartDataQuery(tenantId, days ?? 30, platformCode), ct);
            return Results.Ok(result);
        })
        .WithName("GetSalesChartData")
        .WithSummary("Satış grafiği verisi — gün + platform filtreli")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/service-health — altyapı servisleri sağlık durumu (G308-DEV6)
        group.MapGet("/service-health", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetServiceHealthQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetServiceHealth")
        .WithSummary("Altyapı servisleri sağlık durumu — PostgreSQL, Redis, RabbitMQ (G308)")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/dashboard/app-hub — unified aggregator (G207-DEV6)
        group.MapGet("/app-hub", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAppHubDataQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetAppHubData")
        .WithSummary("AppHub dashboard aggregator — KPI + health + alerts tek response (G207)")
        .Produces(200)
        .CacheOutput("Dashboard30s");
    }
}
