using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services.Reporting;

/// <summary>
/// S2-DEV3-01: KPI toplama servisi — her 15 dk Hangfire ile snapshot alır.
/// GL entry'lerden ciro, DB'den sipariş/stok/iade sayıları.
/// Platform bazlı aggregation destekli.
/// </summary>
public interface IKpiAggregationService
{
    Task<KpiSnapshotDto> CollectCurrentKpisAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<KpiSnapshotDto>> GetTrendAsync(Guid tenantId, int days = 30, CancellationToken ct = default);
}

public sealed class KpiAggregationService : IKpiAggregationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<KpiAggregationService> _logger;

    public KpiAggregationService(AppDbContext db, ILogger<KpiAggregationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<KpiSnapshotDto> CollectCurrentKpisAsync(Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("[KPI] Snapshot toplama başlıyor — TenantId={TenantId}", tenantId);

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Sipariş metrikleri
        var orderStats = await _db.Orders
            .Where(o => o.TenantId == tenantId && o.OrderDate >= monthStart)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount),
                AvgOrderValue = g.Average(o => o.TotalAmount)
            })
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        // Bugünkü siparişler
        var todayOrders = await _db.Orders
            .CountAsync(o => o.TenantId == tenantId && o.OrderDate >= today, ct)
            .ConfigureAwait(false);

        // Stok metrikleri
        var stockStats = await _db.Products
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalProducts = g.Count(),
                InStock = g.Count(p => p.Stock > 0),
                OutOfStock = g.Count(p => p.Stock <= 0),
                TotalStockValue = g.Sum(p => p.Stock * p.SalePrice),
                LowStock = g.Count(p => p.Stock > 0 && p.Stock <= p.MinimumStock && p.MinimumStock > 0)
            })
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        var snapshot = new KpiSnapshotDto
        {
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
            // Sipariş
            MonthlyOrderCount = orderStats?.Count ?? 0,
            MonthlyRevenue = orderStats?.TotalRevenue ?? 0,
            AverageOrderValue = orderStats?.AvgOrderValue ?? 0,
            TodayOrderCount = todayOrders,
            // Stok
            TotalActiveProducts = stockStats?.TotalProducts ?? 0,
            InStockProducts = stockStats?.InStock ?? 0,
            OutOfStockProducts = stockStats?.OutOfStock ?? 0,
            LowStockProducts = stockStats?.LowStock ?? 0,
            TotalStockValue = stockStats?.TotalStockValue ?? 0
        };

        _logger.LogInformation(
            "[KPI] Snapshot: Orders={Orders}, Revenue={Revenue:F2}, InStock={InStock}/{Total}",
            snapshot.MonthlyOrderCount, snapshot.MonthlyRevenue,
            snapshot.InStockProducts, snapshot.TotalActiveProducts);

        return snapshot;
    }

    public async Task<IReadOnlyList<KpiSnapshotDto>> GetTrendAsync(Guid tenantId, int days = 30, CancellationToken ct = default)
    {
        // Günlük sipariş trendi — son N gün
        var since = DateTime.UtcNow.Date.AddDays(-days);

        var dailyOrders = await _db.Orders
            .Where(o => o.TenantId == tenantId && o.OrderDate >= since)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new KpiSnapshotDto
            {
                TenantId = tenantId,
                Timestamp = g.Key,
                TodayOrderCount = g.Count(),
                MonthlyRevenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(s => s.Timestamp)
            .ToListAsync(ct).ConfigureAwait(false);

        return dailyOrders.AsReadOnly();
    }
}

/// <summary>
/// KPI snapshot DTO — dashboard ve trend için kullanılır.
/// </summary>
public sealed class KpiSnapshotDto
{
    public Guid TenantId { get; set; }
    public DateTime Timestamp { get; set; }

    // Sipariş
    public int MonthlyOrderCount { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int TodayOrderCount { get; set; }

    // Stok
    public int TotalActiveProducts { get; set; }
    public int InStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; }
    public decimal TotalStockValue { get; set; }
}
