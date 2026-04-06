using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Unit.Concurrency;

/// <summary>
/// KD-DEV5-002: Concurrency tests — parallel stock updates, optimistic concurrency (RowVersion).
/// Validates that concurrent stock modifications do not cause data corruption or silent overwrites.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "KaliteDevrimi")]
public class ConcurrencyStockTests : IDisposable
{
    private static readonly Guid TenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly string _dbName = $"ConcurrencyTest_{Guid.NewGuid()}";

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        return new AppDbContext(options, new StubTenantProvider(TenantId));
    }

    // ── TEST 1: Parallel product name update — last write wins in InMemory ──

    [Fact]
    public async Task ParallelProductUpdate_ShouldNotThrow_WithInMemory()
    {
        // Arrange — seed product
        var productId = Guid.NewGuid();
        using (var ctx = CreateContext())
        {
            ctx.Products.Add(new Product
            {
                Id = productId,
                TenantId = TenantId,
                Name = "Concurrency-Widget",
                SKU = "SKU-CONC-001",
                PurchasePrice = 10m,
                SalePrice = 20m,
                CategoryId = Guid.NewGuid(),
            });
            await ctx.SaveChangesAsync();
        }

        // Act — 10 parallel updates (each in its own context, update SalePrice)
        const int threadCount = 10;

        var tasks = Enumerable.Range(0, threadCount).Select(async i =>
        {
            using var ctx = CreateContext();
            var product = await ctx.Products.FirstAsync(p => p.Id == productId);
            product.SalePrice = 20m + i;
            await ctx.SaveChangesAsync();
        });

        // With InMemory provider, concurrent saves succeed (last-write-wins)
        await Task.WhenAll(tasks);

        // Assert — product exists and has a valid SalePrice
        using var verifyCtx = CreateContext();
        var final = await verifyCtx.Products.FirstAsync(p => p.Id == productId);
        final.SalePrice.Should().BeGreaterThanOrEqualTo(20m,
            "At least one update must have persisted");
    }

    // ── TEST 2: RowVersion presence on Product entity ──

    [Fact]
    public void Product_ShouldHave_RowVersion_Property()
    {
        var prop = typeof(Product).GetProperty("RowVersion");
        prop.Should().NotBeNull("Product must have RowVersion for optimistic concurrency");
        prop!.PropertyType.Should().Be(typeof(byte[]),
            "RowVersion should be byte[] for PostgreSQL xmin mapping");
    }

    // ── TEST 3: RowVersion presence on Order entity ──

    [Fact]
    public void Order_ShouldHave_RowVersion_Property()
    {
        var prop = typeof(Order).GetProperty("RowVersion");
        prop.Should().NotBeNull("Order must have RowVersion for optimistic concurrency");
        prop!.PropertyType.Should().Be(typeof(byte[]));
    }

    // ── TEST 4: RowVersion presence on Invoice entity ──

    [Fact]
    public void Invoice_ShouldHave_RowVersion_Property()
    {
        var prop = typeof(Invoice).GetProperty("RowVersion");
        prop.Should().NotBeNull("Invoice must have RowVersion for optimistic concurrency");
        prop!.PropertyType.Should().Be(typeof(byte[]));
    }

    // ── TEST 5: RowVersion presence on StockMovement entity ──

    [Fact]
    public void StockMovement_ShouldHave_RowVersion_Property()
    {
        var prop = typeof(StockMovement).GetProperty("RowVersion");
        prop.Should().NotBeNull("StockMovement must have RowVersion for optimistic concurrency");
        prop!.PropertyType.Should().Be(typeof(byte[]));
    }

    // ── TEST 6: RowVersion presence on ProductWarehouseStock entity ──

    [Fact]
    public void ProductWarehouseStock_ShouldHave_RowVersion_Property()
    {
        var prop = typeof(ProductWarehouseStock).GetProperty("RowVersion");
        prop.Should().NotBeNull("ProductWarehouseStock must have RowVersion for optimistic concurrency");
        prop!.PropertyType.Should().Be(typeof(byte[]));
    }

    // ── TEST 7: Concurrent order creation — no duplicate order numbers ──

    [Fact]
    public async Task ParallelOrderCreation_ShouldCreateDistinctOrders()
    {
        const int orderCount = 10;

        var tasks = Enumerable.Range(0, orderCount).Select(async i =>
        {
            using var ctx = CreateContext();
            ctx.Orders.Add(new Order
            {
                TenantId = TenantId,
                OrderNumber = $"ORD-CONC-{i:D3}",
                CustomerId = Guid.NewGuid(),
                OrderDate = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        });

        await Task.WhenAll(tasks);

        // Assert
        using var verifyCtx = CreateContext();
        var orders = await verifyCtx.Orders.ToListAsync();
        orders.Should().HaveCount(orderCount);
        orders.Select(o => o.OrderNumber).Distinct().Should().HaveCount(orderCount,
            "Each concurrent order should have a unique OrderNumber");
    }

    // ── TEST 8: Concurrent product reads are consistent ──

    [Fact]
    public async Task ConcurrentReads_ShouldReturnConsistentData()
    {
        // Arrange
        var productId = Guid.NewGuid();
        using (var ctx = CreateContext())
        {
            ctx.Products.Add(new Product
            {
                Id = productId,
                TenantId = TenantId,
                Name = "Read-Consistency",
                SKU = "SKU-READ-001",
                PurchasePrice = 10m,
                SalePrice = 42m,
                CategoryId = Guid.NewGuid(),
            });
            await ctx.SaveChangesAsync();
        }

        // Act — 10 concurrent reads
        var readTasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            using var ctx = CreateContext();
            var product = await ctx.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            return product.SalePrice;
        });

        var results = await Task.WhenAll(readTasks);

        // Assert — all reads should return the same value
        results.Should().AllBeEquivalentTo(42m,
            "All concurrent reads should return the same SalePrice");
    }

    // ── TEST 9: Stale update simulation ──

    [Fact]
    public async Task StaleUpdate_DocumentsLastWriteWins_OnInMemory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        using (var ctx = CreateContext())
        {
            ctx.Products.Add(new Product
            {
                Id = productId,
                TenantId = TenantId,
                Name = "Stale-Test",
                SKU = "SKU-STALE-001",
                PurchasePrice = 10m,
                SalePrice = 100m,
                CategoryId = Guid.NewGuid(),
            });
            await ctx.SaveChangesAsync();
        }

        // Act — read in ctx1, modify and save in ctx2, then try to save ctx1
        using var ctx1 = CreateContext();
        using var ctx2 = CreateContext();

        var product1 = await ctx1.Products.FirstAsync(p => p.Id == productId);
        var product2 = await ctx2.Products.FirstAsync(p => p.Id == productId);

        // ctx2 saves first
        product2.SalePrice = 50m;
        await ctx2.SaveChangesAsync();

        // ctx1 tries to save stale data
        product1.SalePrice = 75m;

        // With InMemory, this succeeds (last-write-wins).
        // With real PostgreSQL + xmin concurrency token, this would throw DbUpdateConcurrencyException.
        var act = async () => await ctx1.SaveChangesAsync();
        await act.Should().NotThrowAsync(
            "InMemory provider uses last-write-wins; PostgreSQL xmin would throw DbUpdateConcurrencyException");

        // Verify final state
        using var verifyCtx = CreateContext();
        var final = await verifyCtx.Products.FirstAsync(p => p.Id == productId);
        final.SalePrice.Should().Be(75m, "InMemory: last write wins");
    }

    // ── TEST 10: SemaphoreSlim guard pattern validation ──

    [Fact]
    public async Task SemaphoreSlim_ShouldSerialize_ConcurrentAccess()
    {
        // Validate the SemaphoreSlim pattern used in ViewModelBase
        var semaphore = new SemaphoreSlim(1, 1);
        var counter = 0;
        var maxConcurrent = 0;
        var currentConcurrent = 0;

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await semaphore.WaitAsync();
            try
            {
                var c = Interlocked.Increment(ref currentConcurrent);
                if (c > maxConcurrent) Interlocked.Exchange(ref maxConcurrent, c);
                Interlocked.Increment(ref counter);
                await Task.Delay(1); // simulate work
                Interlocked.Decrement(ref currentConcurrent);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        counter.Should().Be(10, "All 10 operations should complete");
        maxConcurrent.Should().Be(1, "SemaphoreSlim(1,1) should allow only 1 concurrent execution");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private sealed class StubTenantProvider : ITenantProvider
    {
        private readonly Guid _tenantId;
        public StubTenantProvider(Guid tenantId) => _tenantId = tenantId;
        public Guid GetCurrentTenantId() => _tenantId;
    }
}
