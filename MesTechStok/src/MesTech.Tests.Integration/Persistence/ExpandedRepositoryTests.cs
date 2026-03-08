using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// 5.D2-05: Expanded Testcontainers repository tests.
/// Covers: Category, Order, Warehouse repositories + Product edge cases.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class ExpandedRepositoryTests : IClassFixture<PostgreSqlContainerFixture>, IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private AppDbContext _context = null!;
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private Guid _categoryId;
    private Guid _customerId;

    public ExpandedRepositoryTests(PostgreSqlContainerFixture fixture) => _fixture = fixture;

    private DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

    public async Task InitializeAsync()
    {
        _context = new AppDbContext(CreateOptions(), new TestTenantProvider());
        await _context.Database.MigrateAsync();

        var tenantExists = await _context.Set<Tenant>().IgnoreQueryFilters()
            .AnyAsync(t => t.Id == TestTenantId);
        if (!tenantExists)
        {
            var tenant = new Tenant { Name = "Test Tenant", TaxNumber = "1234567890", IsActive = true };
            typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, TestTenantId);
            _context.Set<Tenant>().Add(tenant);
            await _context.SaveChangesAsync();
        }

        var cat = new Category
        {
            Name = "Expanded Test",
            Code = $"EXP-{Guid.NewGuid().ToString()[..6]}",
            TenantId = TestTenantId,
            IsActive = true
        };
        _context.Set<Category>().Add(cat);
        await _context.SaveChangesAsync();
        _categoryId = cat.Id;

        var customer = new Customer
        {
            Name = "Test Customer",
            Code = $"CUS-{Guid.NewGuid().ToString()[..6]}",
            TenantId = TestTenantId,
        };
        _context.Set<Customer>().Add(customer);
        await _context.SaveChangesAsync();
        _customerId = customer.Id;
    }

    public async Task DisposeAsync()
    {
        _context.Set<Order>().RemoveRange(_context.Set<Order>().IgnoreQueryFilters());
        await _context.SaveChangesAsync();
        _context.Products.RemoveRange(_context.Products.IgnoreQueryFilters());
        await _context.SaveChangesAsync();
        _context.Warehouses.RemoveRange(_context.Warehouses.IgnoreQueryFilters());
        await _context.SaveChangesAsync();
        _context.Set<Customer>().RemoveRange(
            _context.Set<Customer>().IgnoreQueryFilters().Where(c => c.Id == _customerId));
        await _context.SaveChangesAsync();
        _context.Set<Category>().RemoveRange(
            _context.Set<Category>().IgnoreQueryFilters().Where(c => c.Id == _categoryId));
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    // ── CategoryRepository ──

    [Fact]
    public async Task Category_Crud_ShouldWorkWithRealPostgres()
    {
        var repo = new CategoryRepository(_context);
        var cat = new Category
        {
            Name = "Test Cat",
            Code = $"CAT-{Guid.NewGuid().ToString()[..6]}",
            TenantId = TestTenantId,
            IsActive = true,
            SortOrder = 1
        };
        await repo.AddAsync(cat);
        await _context.SaveChangesAsync();

        var found = await repo.GetByIdAsync(cat.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Test Cat");

        // Clean up
        _context.Set<Category>().Remove(cat);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Category_GetActive_ShouldFilterInactive()
    {
        var repo = new CategoryRepository(_context);
        var active = new Category
        {
            Name = "Active Cat",
            Code = $"ACT-{Guid.NewGuid().ToString()[..6]}",
            TenantId = TestTenantId,
            IsActive = true,
            SortOrder = 1
        };
        var inactive = new Category
        {
            Name = "Inactive Cat",
            Code = $"INA-{Guid.NewGuid().ToString()[..6]}",
            TenantId = TestTenantId,
            IsActive = false,
            SortOrder = 2
        };
        await repo.AddAsync(active);
        await repo.AddAsync(inactive);
        await _context.SaveChangesAsync();

        var result = await repo.GetActiveAsync();
        result.Should().Contain(c => c.Id == active.Id);
        result.Should().NotContain(c => c.Id == inactive.Id);

        _context.Set<Category>().RemoveRange(active, inactive);
        await _context.SaveChangesAsync();
    }

    // ── OrderRepository ──

    [Fact]
    public async Task Order_CreateAndRetrieve_ShouldWorkWithRealPostgres()
    {
        var repo = new OrderRepository(_context);
        var order = new Order
        {
            OrderNumber = $"ORD-TEST-{Guid.NewGuid().ToString()[..6]}",
            CustomerId = _customerId,
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            TenantId = TestTenantId,
        };
        await repo.AddAsync(order);
        await _context.SaveChangesAsync();

        var found = await repo.GetByIdAsync(order.Id);
        found.Should().NotBeNull();
        found!.CustomerName.Should().Be("Test Customer");

        var byNumber = await repo.GetByOrderNumberAsync(order.OrderNumber);
        byNumber.Should().NotBeNull();
        byNumber!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Order_GetByDateRange_ShouldFilterCorrectly()
    {
        var repo = new OrderRepository(_context);
        var now = DateTime.UtcNow;
        var order1 = new Order
        {
            OrderNumber = $"DR-1-{Guid.NewGuid().ToString()[..6]}",
            CustomerId = _customerId,
            OrderDate = now.AddDays(-5),
            TenantId = TestTenantId
        };
        var order2 = new Order
        {
            OrderNumber = $"DR-2-{Guid.NewGuid().ToString()[..6]}",
            CustomerId = _customerId,
            OrderDate = now.AddDays(-30),
            TenantId = TestTenantId
        };
        await repo.AddAsync(order1);
        await repo.AddAsync(order2);
        await _context.SaveChangesAsync();

        var recent = await repo.GetByDateRangeAsync(now.AddDays(-10), now);
        recent.Should().Contain(o => o.Id == order1.Id);
        recent.Should().NotContain(o => o.Id == order2.Id);
    }

    [Fact]
    public async Task Order_GetCount_ShouldReturnCorrectCount()
    {
        var repo = new OrderRepository(_context);
        var initialCount = await repo.GetCountAsync();

        await repo.AddAsync(new Order
        {
            OrderNumber = $"CNT-{Guid.NewGuid().ToString()[..6]}",
            CustomerId = _customerId,
            TenantId = TestTenantId
        });
        await _context.SaveChangesAsync();

        var newCount = await repo.GetCountAsync();
        newCount.Should().Be(initialCount + 1);
    }

    // ── WarehouseRepository ──

    [Fact]
    public async Task Warehouse_Crud_ShouldWork()
    {
        var repo = new WarehouseRepository(_context);
        var wh = new Warehouse
        {
            Name = "Test Warehouse",
            Code = $"WH-{Guid.NewGuid().ToString()[..6]}",
            Type = "MAIN",
            TenantId = TestTenantId,
            IsActive = true,
            IsDefault = false
        };
        await repo.AddAsync(wh);
        await _context.SaveChangesAsync();

        var found = await repo.GetByIdAsync(wh.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Test Warehouse");
    }

    [Fact]
    public async Task Warehouse_GetDefault_ShouldReturnDefaultWarehouse()
    {
        var repo = new WarehouseRepository(_context);
        var defaultWh = new Warehouse
        {
            Name = "Default WH",
            Code = $"DWH-{Guid.NewGuid().ToString()[..6]}",
            Type = "MAIN",
            TenantId = TestTenantId,
            IsActive = true,
            IsDefault = true
        };
        await repo.AddAsync(defaultWh);
        await _context.SaveChangesAsync();

        var result = await repo.GetDefaultAsync();
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    // ── ProductRepository Edge Cases ──

    [Fact]
    public async Task Product_GetLowStock_ShouldReturnOnlyLowStockProducts()
    {
        var repo = new ProductRepository(_context);
        _context.Products.AddRange(
            new Product
            {
                Name = "Low Stock",
                SKU = $"LS-{Guid.NewGuid().ToString()[..6]}",
                Stock = 2,
                MinimumStock = 10,
                CategoryId = _categoryId,
                TenantId = TestTenantId,
                IsActive = true
            },
            new Product
            {
                Name = "Normal Stock",
                SKU = $"NS-{Guid.NewGuid().ToString()[..6]}",
                Stock = 50,
                MinimumStock = 10,
                CategoryId = _categoryId,
                TenantId = TestTenantId,
                IsActive = true
            });
        await _context.SaveChangesAsync();

        var lowStock = await repo.GetLowStockAsync();
        lowStock.Should().Contain(p => p.Name == "Low Stock");
        lowStock.Should().NotContain(p => p.Name == "Normal Stock");
    }

    [Fact]
    public async Task Product_GetByCategory_ShouldFilterCorrectly()
    {
        var repo = new ProductRepository(_context);
        var otherCat = new Category
        {
            Name = "Other",
            Code = $"OTH-{Guid.NewGuid().ToString()[..6]}",
            TenantId = TestTenantId,
            IsActive = true
        };
        _context.Set<Category>().Add(otherCat);
        await _context.SaveChangesAsync();

        _context.Products.AddRange(
            new Product
            {
                Name = "In Category",
                SKU = $"IC-{Guid.NewGuid().ToString()[..6]}",
                CategoryId = _categoryId,
                TenantId = TestTenantId,
                IsActive = true
            },
            new Product
            {
                Name = "Other Category",
                SKU = $"OC-{Guid.NewGuid().ToString()[..6]}",
                CategoryId = otherCat.Id,
                TenantId = TestTenantId,
                IsActive = true
            });
        await _context.SaveChangesAsync();

        var result = await repo.GetByCategoryAsync(_categoryId);
        result.Should().Contain(p => p.Name == "In Category");
        result.Should().NotContain(p => p.Name == "Other Category");

        _context.Set<Category>().Remove(otherCat);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Product_GetCount_ShouldWork()
    {
        var repo = new ProductRepository(_context);
        var initialCount = await repo.GetCountAsync();

        _context.Products.Add(new Product
        {
            Name = "Count Test",
            SKU = $"CT-{Guid.NewGuid().ToString()[..6]}",
            CategoryId = _categoryId,
            TenantId = TestTenantId,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var newCount = await repo.GetCountAsync();
        newCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task Product_GetBySKU_ShouldFindExact()
    {
        var repo = new ProductRepository(_context);
        var uniqueSku = $"EXACT-{Guid.NewGuid().ToString()[..8]}";
        _context.Products.Add(new Product
        {
            Name = "SKU Test",
            SKU = uniqueSku,
            CategoryId = _categoryId,
            TenantId = TestTenantId,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var found = await repo.GetBySKUAsync(uniqueSku);
        found.Should().NotBeNull();
        found!.SKU.Should().Be(uniqueSku);

        var notFound = await repo.GetBySKUAsync("NONEXISTENT-SKU");
        notFound.Should().BeNull();
    }

    private sealed class TestTenantProvider : ITenantProvider
    {
        public Guid GetCurrentTenantId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
