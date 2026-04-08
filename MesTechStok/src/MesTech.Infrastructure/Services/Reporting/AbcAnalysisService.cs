using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services.Reporting;

/// <summary>
/// S2-DEV3-02: ABC analiz servisi — 80/15/5 kuralı ile ürün sınıflandırma.
/// Son 90 gün satış verisinden hesaplanır.
/// A: %80 ciro (en değerli), B: %15, C: %5 (en az değerli).
/// </summary>
public interface IAbcAnalysisService
{
    Task<AbcAnalysisResult> AnalyzeAsync(Guid tenantId, int days = 90, CancellationToken ct = default);
}

public sealed class AbcAnalysisService : IAbcAnalysisService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AbcAnalysisService> _logger;

    public AbcAnalysisService(AppDbContext db, ILogger<AbcAnalysisService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AbcAnalysisResult> AnalyzeAsync(Guid tenantId, int days = 90, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        // Ürün bazlı satış toplamı (OrderItem + Order join)
        var productSales = await _db.Set<Domain.Entities.OrderItem>()
            .Join(_db.Orders,
                oi => oi.OrderId,
                o => o.Id,
                (oi, o) => new { oi, o })
            .Where(x => x.oi.TenantId == tenantId && x.o.OrderDate >= since)
            .GroupBy(x => new { x.oi.ProductId, x.oi.ProductSKU, x.oi.ProductName })
            .Select(g => new ProductSalesDto
            {
                ProductId = g.Key.ProductId,
                SKU = g.Key.ProductSKU,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(x => x.oi.Quantity),
                TotalRevenue = g.Sum(x => x.oi.Quantity * x.oi.UnitPrice)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .ToListAsync(ct).ConfigureAwait(false);

        if (productSales.Count == 0)
        {
            return new AbcAnalysisResult
            {
                TenantId = tenantId,
                AnalysisDate = DateTime.UtcNow,
                PeriodDays = days
            };
        }

        var totalRevenue = productSales.Sum(p => p.TotalRevenue);
        if (totalRevenue <= 0)
        {
            foreach (var p in productSales) p.Category = AbcCategory.C;
            return new AbcAnalysisResult
            {
                TenantId = tenantId,
                AnalysisDate = DateTime.UtcNow,
                PeriodDays = days,
                Products = productSales
            };
        }

        // Kümülatif ciro ile ABC sınıflandırma
        decimal cumulative = 0;
        foreach (var product in productSales)
        {
            cumulative += product.TotalRevenue;
            var pct = cumulative / totalRevenue;

            product.RevenuePercentage = product.TotalRevenue / totalRevenue * 100;
            product.CumulativePercentage = pct * 100;
            product.Category = pct <= 0.80m ? AbcCategory.A
                : pct <= 0.95m ? AbcCategory.B
                : AbcCategory.C;
        }

        var result = new AbcAnalysisResult
        {
            TenantId = tenantId,
            AnalysisDate = DateTime.UtcNow,
            PeriodDays = days,
            TotalRevenue = totalRevenue,
            Products = productSales,
            CategoryA_Count = productSales.Count(p => p.Category == AbcCategory.A),
            CategoryB_Count = productSales.Count(p => p.Category == AbcCategory.B),
            CategoryC_Count = productSales.Count(p => p.Category == AbcCategory.C)
        };

        _logger.LogInformation(
            "[ABC] Analiz: {Total} ürün, A={A} B={B} C={C}, ToplamCiro={Revenue:F2}",
            productSales.Count, result.CategoryA_Count, result.CategoryB_Count,
            result.CategoryC_Count, totalRevenue);

        return result;
    }
}

public enum AbcCategory { A, B, C }

public sealed class ProductSalesDto
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenuePercentage { get; set; }
    public decimal CumulativePercentage { get; set; }
    public AbcCategory Category { get; set; }
}

public sealed class AbcAnalysisResult
{
    public Guid TenantId { get; set; }
    public DateTime AnalysisDate { get; set; }
    public int PeriodDays { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CategoryA_Count { get; set; }
    public int CategoryB_Count { get; set; }
    public int CategoryC_Count { get; set; }
    public List<ProductSalesDto> Products { get; set; } = new();
}
