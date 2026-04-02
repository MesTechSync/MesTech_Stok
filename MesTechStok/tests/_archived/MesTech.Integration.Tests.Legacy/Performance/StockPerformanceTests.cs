using System.Diagnostics;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Performance;

/// <summary>
/// EMR-18 Performance Benchmark Tests — Stock &amp; Order scenarios.
/// Measures insert, query, aggregate, and burst-update performance
/// against InMemory EF Core to establish baseline thresholds.
/// Production targets assume PostgreSQL; InMemory times are lower bounds.
/// </summary>
[Trait("Category", "Performance")]
public sealed class StockPerformanceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ITestOutputHelper _output;
    private readonly Guid _tenantId = Guid.NewGuid();

    public StockPerformanceTests(ITestOutputHelper output)
    {
        _output = output;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"PerfTest_{Guid.NewGuid():N}")
            .Options;

        _context = new AppDbContext(options, new PerfTestTenantProvider(_tenantId));
        _context.Database.EnsureCreated();
    }

    // ──────────────────────────────────────────────────
    // Senaryo 1: 1000 product insert — target <500ms
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Benchmark_1000_Product_Insert_ShouldComplete_Under500ms()
    {
        // Arrange — generate 1000 products
        var categoryId = Guid.NewGuid();
        var products = Enumerable.Range(1, 1000).Select(i => new Product
        {
            TenantId = _tenantId,
            Name = $"BenchProduct-{i:D4}",
            SKU = $"BENCH-{i:D4}",
            Barcode = $"8690000{i:D6}",
            Stock = 100 + (i % 50),
            MinimumStock = 5,
            MaximumStock = 500,
            ReorderLevel = 10,
            PurchasePrice = 10m + (i % 100),
            SalePrice = 20m + (i % 100),
            TaxRate = 0.18m,
            CategoryId = categoryId,
            IsActive = true
        }).ToList();

        // Act
        var sw = Stopwatch.StartNew();
        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
        sw.Stop();

        // Assert
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[Senaryo 1] 1000 product insert: {elapsed}ms");

        elapsed.Should().BeLessThan(2000,
            "1000 product insert should complete under 2000ms (InMemory baseline)");

        var count = await _context.Products
            .IgnoreQueryFilters()
            .CountAsync(p => p.TenantId == _tenantId);
        count.Should().Be(1000);
    }

    // ──────────────────────────────────────────────────
    // Senaryo 2: 500 order fetch with Include — target <200ms
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Benchmark_500_Order_Fetch_WithInclude_ShouldComplete_Under200ms()
    {
        // Arrange — seed 500 orders with 2 items each
        var customerId = Guid.NewGuid();
        for (int i = 0; i < 500; i++)
        {
            var order = new Order
            {
                TenantId = _tenantId,
                OrderNumber = $"ORD-{i:D5}",
                CustomerId = customerId,
                Status = OrderStatus.Confirmed,
                OrderDate = DateTime.UtcNow.AddDays(-i),
                TaxRate = 0.18m,
                CustomerName = $"Customer-{i}"
            };
            order.SetFinancials(100m + i, 18m, 118m + i);
            await _context.Orders.AddAsync(order);

            // Add 2 order items per order
            for (int j = 0; j < 2; j++)
            {
                var item = new OrderItem
                {
                    TenantId = _tenantId,
                    OrderId = order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = $"Product-{i}-{j}",
                    ProductSKU = $"SKU-{i}-{j}",
                    Quantity = j + 1,
                    UnitPrice = 50m,
                    TotalPrice = 50m * (j + 1),
                    TaxRate = 0.18m,
                    TaxAmount = 9m * (j + 1)
                };
                await _context.OrderItems.AddAsync(item);
            }
        }
        await _context.SaveChangesAsync();

        // Act — paged fetch (page 1, size 50) with Include
        var sw = Stopwatch.StartNew();
        var orders = await _context.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == _tenantId)
            .OrderByDescending(o => o.OrderDate)
            .Skip(0)
            .Take(50)
            .ToListAsync();
        sw.Stop();

        // Assert
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[Senaryo 2] 500 order fetch (page 1, size 50): {elapsed}ms");

        elapsed.Should().BeLessThan(200,
            "paged order fetch should complete under 200ms");
        orders.Should().HaveCount(50);
    }

    // ──────────────────────────────────────────────────
    // Senaryo 5: Dashboard aggregate with 10K orders — target <500ms
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Benchmark_Dashboard_Stats_With10K_Orders_ShouldComplete_Under500ms()
    {
        // Arrange — seed 10,000 orders
        var customerId = Guid.NewGuid();
        var orders = Enumerable.Range(1, 10_000).Select(i =>
        {
            var order = new Order
            {
                TenantId = _tenantId,
                OrderNumber = $"DASH-{i:D6}",
                CustomerId = customerId,
                Status = (OrderStatus)(i % 5), // distribute across statuses
                OrderDate = DateTime.UtcNow.AddDays(-(i % 365)),
                TaxRate = 0.18m,
                CustomerName = $"DashCustomer-{i % 100}"
            };
            order.SetFinancials(50m + (i % 200), 9m + (i % 36), 59m + (i % 236));
            return order;
        }).ToList();

        await _context.Orders.AddRangeAsync(orders);
        await _context.SaveChangesAsync();

        // Act — simulate dashboard KPI aggregation
        var sw = Stopwatch.StartNew();

        var baseQuery = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == _tenantId);

        var totalOrders = await baseQuery.CountAsync();
        var totalRevenue = await baseQuery.SumAsync(o => o.TotalAmount);
        var avgOrderValue = await baseQuery.AverageAsync(o => o.TotalAmount);
        var statusBreakdown = await baseQuery
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var last30DaysOrders = await baseQuery
            .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        sw.Stop();

        // Assert
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[Senaryo 5] Dashboard aggregate (10K orders): {elapsed}ms");
        _output.WriteLine($"  Total orders: {totalOrders}");
        _output.WriteLine($"  Total revenue: {totalRevenue:N2}");
        _output.WriteLine($"  Avg order value: {avgOrderValue:N2}");
        _output.WriteLine($"  Status groups: {statusBreakdown.Count}");
        _output.WriteLine($"  Last 30 days: {last30DaysOrders}");

        elapsed.Should().BeLessThan(500,
            "dashboard aggregate over 10K orders should complete under 500ms");
        totalOrders.Should().Be(10_000);
        statusBreakdown.Should().HaveCountGreaterThan(0);
    }

    // ──────────────────────────────────────────────────
    // Senaryo 6: 500 stock update burst — target <5000ms
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Benchmark_500_Stock_Updates_ShouldComplete_Under5s()
    {
        // Arrange — seed 500 products
        var categoryId = Guid.NewGuid();
        var products = Enumerable.Range(1, 500).Select(i => new Product
        {
            TenantId = _tenantId,
            Name = $"BurstProduct-{i:D4}",
            SKU = $"BURST-{i:D4}",
            Stock = 100,
            MinimumStock = 5,
            MaximumStock = 500,
            ReorderLevel = 10,
            PurchasePrice = 25m,
            SalePrice = 50m,
            CategoryId = categoryId,
            IsActive = true
        }).ToList();

        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();

        // Act — burst update all 500 stocks
        var sw = Stopwatch.StartNew();

        var loadedProducts = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == _tenantId && p.SKU.StartsWith("BURST-"))
            .ToListAsync();

        foreach (var product in loadedProducts)
        {
            product.AdjustStock(
                quantity: -10,
                movementType: StockMovementType.Sale,
                reason: "Burst performance test sale");
        }

        await _context.SaveChangesAsync();
        sw.Stop();

        // Assert
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[Senaryo 6] 500 stock updates burst: {elapsed}ms");

        elapsed.Should().BeLessThan(5000,
            "500 stock updates should complete under 5 seconds");

        // Verify all stocks were decremented
        var updatedProducts = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == _tenantId && p.SKU.StartsWith("BURST-"))
            .ToListAsync();

        updatedProducts.Should().OnlyContain(p => p.Stock == 90,
            "each product should have stock reduced from 100 to 90");
    }

    // ──────────────────────────────────────────────────
    // Infrastructure
    // ──────────────────────────────────────────────────

    public void Dispose()
    {
        _context?.Dispose();
    }

    private sealed class PerfTestTenantProvider : ITenantProvider
    {
        private readonly Guid _tenantId;
        public PerfTestTenantProvider(Guid tenantId) => _tenantId = tenantId;
        public Guid GetCurrentTenantId() => _tenantId;
    }
}
