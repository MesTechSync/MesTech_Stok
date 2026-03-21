using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// 12-KPI unified dashboard özeti — AppDbContext ile direkt EF Core sorguları.
/// Mevcut 6 dashboard handler ile çakışmaz.
/// ⚠️ Doğrulanmış property adları: Product.Stock (StockQuantity değil),
///    ReturnRequests (Returns değil), Order.OrderDate (tarihleme için).
/// </summary>
public class DashboardSummaryRepository : IDashboardSummaryRepository
{
    private readonly AppDbContext _db;

    public DashboardSummaryRepository(AppDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var weekAgo = today.AddDays(-7);
        var tid = tenantId;

        var dto = new DashboardSummaryDto();

        // ── Satır 1: Ana metrikler ──────────────────────────────────────────
        var todayOrders = _db.Orders
            .Where(o => o.TenantId == tid && o.OrderDate >= today && !o.IsDeleted);
        dto.TodayOrderCount = await todayOrders.CountAsync(ct);
        dto.TodaySalesAmount = await todayOrders.SumAsync(o => (decimal?)o.TotalAmount ?? 0m, ct);

        dto.ActiveProductCount = await _db.Products
            .CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.IsActive, ct);

        // ⚠️ Product.Stock — "StockQuantity" değil
        dto.CriticalStockCount = await _db.Products
            .CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Stock <= p.MinimumStock, ct);

        // ── Satır 2: Platform metrikleri ────────────────────────────────────
        dto.ActivePlatformCount = await _db.Stores
            .Where(s => s.TenantId == tid && s.IsActive && !s.IsDeleted)
            .Select(s => s.PlatformType)
            .Distinct()
            .CountAsync(ct);

        dto.PendingShipmentCount = await _db.Orders
            .CountAsync(o => o.TenantId == tid && !o.IsDeleted
                         && o.Status == OrderStatus.Confirmed
                         && o.ShippedAt == null, ct);

        var monthOrders = _db.Orders
            .Where(o => o.TenantId == tid && o.OrderDate >= monthStart && !o.IsDeleted);
        dto.MonthlySalesAmount = await monthOrders.SumAsync(o => (decimal?)o.TotalAmount ?? 0m, ct);

        var monthOrderCount = await monthOrders.CountAsync(ct);
        // ⚠️ ReturnRequests — "Returns" değil
        var monthReturnCount = await _db.ReturnRequests
            .CountAsync(r => r.TenantId == tid && r.CreatedAt >= monthStart && !r.IsDeleted, ct);
        dto.ReturnRate = monthOrderCount > 0
            ? Math.Round((decimal)monthReturnCount / monthOrderCount * 100, 1)
            : 0;

        // ── Grafik: Son 7 gün satış trendi ──────────────────────────────────
        dto.Last7DaysSales = await _db.Orders
            .Where(o => o.TenantId == tid && o.OrderDate >= weekAgo && !o.IsDeleted)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new DailySalesPointDto
            {
                Date = g.Key,
                Amount = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .AsNoTracking().ToListAsync(ct);

        // ── Grafik: Platform dağılımı ────────────────────────────────────────
        var pGroups = await _db.Orders
            .Where(o => o.TenantId == tid && o.OrderDate >= monthStart
                     && !o.IsDeleted && o.SourcePlatform != null)
            .GroupBy(o => o.SourcePlatform)
            .Select(g => new { Platform = g.Key, Count = g.Count() })
            .AsNoTracking().ToListAsync(ct);
        var pTotal = pGroups.Sum(g => g.Count);
        dto.PlatformDistribution = pGroups.Select(g => new PlatformOrderDistDto
        {
            PlatformName = g.Platform?.ToString() ?? "Bilinmiyor",
            OrderCount = g.Count,
            Percentage = pTotal > 0 ? Math.Round((decimal)g.Count / pTotal * 100, 1) : 0
        }).ToList();

        // ── Tablo: Son 10 sipariş ────────────────────────────────────────────
        dto.RecentOrders = await _db.Orders
            .Where(o => o.TenantId == tid && !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new RecentOrderItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber ?? o.Id.ToString().Substring(0, 8),
                CustomerName = o.CustomerName ?? "—",
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                PlatformName = o.SourcePlatform != null ? o.SourcePlatform.ToString() : null,
                CreatedAt = o.OrderDate
            })
            .AsNoTracking().ToListAsync(ct);

        // ── Tablo: Kritik stok ───────────────────────────────────────────────
        // ⚠️ Product.Stock — "StockQuantity" değil
        dto.CriticalStockItems = await _db.Products
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.Stock <= p.MinimumStock)
            .OrderBy(p => p.Stock - p.MinimumStock)
            .Take(10)
            .Select(p => new CriticalStockItemDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU ?? "—",
                CurrentStock = p.Stock,
                MinimumStock = p.MinimumStock
            })
            .AsNoTracking().ToListAsync(ct);

        return dto;
    }
}
