using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// KD-DEV5-001: Multi-tenant isolation tests for Order, Store, Invoice.
/// Ensures TenantA data is invisible to TenantB queries via global query filters.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "KaliteDevrimi")]
public class MultiTenantIsolationTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly string _dbName = $"MultiTenantIsolation_{Guid.NewGuid()}";

    private AppDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        return new AppDbContext(options, new StubTenantProvider(tenantId));
    }

    // ── ORDER ISOLATION ────────────────────────────────────────────

    [Fact]
    public async Task Order_TenantA_ShouldNotBeVisibleTo_TenantB()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Orders.Add(new Order
            {
                TenantId = TenantA,
                OrderNumber = "ORD-A-001",
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxB = CreateContext(TenantB);
        var ordersB = await ctxB.Orders.ToListAsync();

        // Assert
        ordersB.Should().BeEmpty("TenantB must NOT see TenantA's orders");
    }

    [Fact]
    public async Task Order_EachTenant_ShouldOnlySeeOwnOrders()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Orders.AddRange(
                new Order { TenantId = TenantA, OrderNumber = "ORD-A-010", CustomerId = Guid.NewGuid(), OrderDate = DateTime.UtcNow },
                new Order { TenantId = TenantA, OrderNumber = "ORD-A-011", CustomerId = Guid.NewGuid(), OrderDate = DateTime.UtcNow },
                new Order { TenantId = TenantB, OrderNumber = "ORD-B-020", CustomerId = Guid.NewGuid(), OrderDate = DateTime.UtcNow }
            );
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxA = CreateContext(TenantA);
        using var ctxB = CreateContext(TenantB);

        var listA = await ctxA.Orders.ToListAsync();
        var listB = await ctxB.Orders.ToListAsync();

        // Assert
        listA.Should().HaveCount(2);
        listA.Should().AllSatisfy(o => o.TenantId.Should().Be(TenantA));

        listB.Should().HaveCount(1);
        listB.Single().OrderNumber.Should().Be("ORD-B-020");
    }

    [Fact]
    public async Task Order_SoftDeleted_ShouldNotBeVisible_EvenForCorrectTenant()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Orders.AddRange(
                new Order { TenantId = TenantA, OrderNumber = "ORD-ACT", CustomerId = Guid.NewGuid(), OrderDate = DateTime.UtcNow },
                new Order { TenantId = TenantA, OrderNumber = "ORD-DEL", CustomerId = Guid.NewGuid(), OrderDate = DateTime.UtcNow, IsDeleted = true, DeletedAt = DateTime.UtcNow }
            );
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxA = CreateContext(TenantA);
        var orders = await ctxA.Orders.ToListAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders.Single().OrderNumber.Should().Be("ORD-ACT");
    }

    // ── STORE ISOLATION ────────────────────────────────────────────

    [Fact]
    public async Task Store_TenantA_ShouldNotBeVisibleTo_TenantB()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Stores.Add(new Store
            {
                TenantId = TenantA,
                StoreName = "A-Store",
                PlatformType = PlatformType.Trendyol,
                IsActive = true,
            });
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxB = CreateContext(TenantB);
        var storesB = await ctxB.Stores.ToListAsync();

        // Assert
        storesB.Should().BeEmpty("TenantB must NOT see TenantA's stores");
    }

    [Fact]
    public async Task Store_EachTenant_ShouldOnlySeeOwnStores()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Stores.AddRange(
                new Store { TenantId = TenantA, StoreName = "A-Trendyol", PlatformType = PlatformType.Trendyol },
                new Store { TenantId = TenantB, StoreName = "B-Hepsiburada", PlatformType = PlatformType.Hepsiburada }
            );
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxA = CreateContext(TenantA);
        using var ctxB = CreateContext(TenantB);

        var listA = await ctxA.Stores.ToListAsync();
        var listB = await ctxB.Stores.ToListAsync();

        // Assert
        listA.Should().HaveCount(1);
        listA.Single().StoreName.Should().Be("A-Trendyol");

        listB.Should().HaveCount(1);
        listB.Single().StoreName.Should().Be("B-Hepsiburada");
    }

    // ── INVOICE ISOLATION ──────────────────────────────────────────

    [Fact]
    public async Task Invoice_TenantA_ShouldNotBeVisibleTo_TenantB()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Invoices.Add(new Invoice
            {
                TenantId = TenantA,
                InvoiceNumber = "INV-A-001",
                OrderId = Guid.NewGuid(),
                Type = InvoiceType.EFatura,
                InvoiceDate = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxB = CreateContext(TenantB);
        var invoicesB = await ctxB.Invoices.ToListAsync();

        // Assert
        invoicesB.Should().BeEmpty("TenantB must NOT see TenantA's invoices");
    }

    [Fact]
    public async Task Invoice_EachTenant_ShouldOnlySeeOwnInvoices()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Invoices.AddRange(
                new Invoice { TenantId = TenantA, InvoiceNumber = "INV-A-100", OrderId = Guid.NewGuid(), Type = InvoiceType.EFatura, InvoiceDate = DateTime.UtcNow },
                new Invoice { TenantId = TenantA, InvoiceNumber = "INV-A-101", OrderId = Guid.NewGuid(), Type = InvoiceType.EArsiv, InvoiceDate = DateTime.UtcNow },
                new Invoice { TenantId = TenantB, InvoiceNumber = "INV-B-200", OrderId = Guid.NewGuid(), Type = InvoiceType.EFatura, InvoiceDate = DateTime.UtcNow }
            );
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctxA = CreateContext(TenantA);
        using var ctxB = CreateContext(TenantB);

        var listA = await ctxA.Invoices.ToListAsync();
        var listB = await ctxB.Invoices.ToListAsync();

        // Assert
        listA.Should().HaveCount(2);
        listA.Should().AllSatisfy(i => i.TenantId.Should().Be(TenantA));

        listB.Should().HaveCount(1);
        listB.Single().InvoiceNumber.Should().Be("INV-B-200");
    }

    [Fact]
    public async Task Invoice_Admin_WithIgnoreQueryFilters_ShouldSeeAllTenants()
    {
        // Arrange
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Invoices.AddRange(
                new Invoice { TenantId = TenantA, InvoiceNumber = "INV-ADM-A", OrderId = Guid.NewGuid(), Type = InvoiceType.EFatura, InvoiceDate = DateTime.UtcNow },
                new Invoice { TenantId = TenantB, InvoiceNumber = "INV-ADM-B", OrderId = Guid.NewGuid(), Type = InvoiceType.EFatura, InvoiceDate = DateTime.UtcNow }
            );
            await ctx.SaveChangesAsync();
        }

        // Act
        using var ctx2 = CreateContext(TenantA);
        var all = await ctx2.Invoices.IgnoreQueryFilters().ToListAsync();

        // Assert
        all.Should().HaveCount(2);
        all.Select(i => i.TenantId).Distinct().Should().HaveCount(2,
            "admin bypass should show invoices from both tenants");
    }

    // ── CROSS-ENTITY TENANT LEAK ───────────────────────────────────

    [Fact]
    public async Task CrossEntity_TenantA_CannotAccessAnyTenantB_Data()
    {
        // Arrange — seed all entity types with TenantB data
        using (var ctx = CreateContext(TenantA))
        {
            ctx.Products.Add(new Product { TenantId = TenantB, Name = "B-Prod", SKU = "SKU-B", PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() });
            ctx.Orders.Add(new Order { TenantId = TenantB, OrderNumber = "ORD-B", CustomerId = Guid.NewGuid(), OrderDate = DateTime.UtcNow });
            ctx.Stores.Add(new Store { TenantId = TenantB, StoreName = "B-Store", PlatformType = PlatformType.N11 });
            ctx.Invoices.Add(new Invoice { TenantId = TenantB, InvoiceNumber = "INV-B", OrderId = Guid.NewGuid(), Type = InvoiceType.EFatura, InvoiceDate = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
        }

        // Act — query with TenantA context
        using var ctxA = CreateContext(TenantA);

        var products = await ctxA.Products.ToListAsync();
        var orders = await ctxA.Orders.ToListAsync();
        var stores = await ctxA.Stores.ToListAsync();
        var invoices = await ctxA.Invoices.ToListAsync();

        // Assert — TenantA sees nothing from TenantB
        products.Should().BeEmpty("TenantA must not see TenantB products");
        orders.Should().BeEmpty("TenantA must not see TenantB orders");
        stores.Should().BeEmpty("TenantA must not see TenantB stores");
        invoices.Should().BeEmpty("TenantA must not see TenantB invoices");
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
