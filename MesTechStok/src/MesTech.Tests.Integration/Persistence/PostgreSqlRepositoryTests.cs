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
/// Full-stack repository tests against real PostgreSQL via Testcontainers.
/// Tests: Repository → AppDbContext → Npgsql → PostgreSQL 17.
/// Includes SearchAsync (EF.Functions.ILike — PostgreSQL-only) and concurrent write.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class PostgreSqlRepositoryTests : IClassFixture<PostgreSqlContainerFixture>, IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private AppDbContext _context = null!;
    private ProductRepository _productRepo = null!;
    private StockMovementRepository _movementRepo = null!;

    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private Guid _categoryId;

    public PostgreSqlRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;
    }

    public async Task InitializeAsync()
    {
        _context = new AppDbContext(CreateOptions(), new TestTenantProvider());

        await _context.Database.MigrateAsync();

        // Seed required FK entities: Tenant + Category
        var tenantExists = await _context.Set<Tenant>().IgnoreQueryFilters()
            .AnyAsync(t => t.Id == TestTenantId);
        if (!tenantExists)
        {
            var tenant = new Tenant { Name = "Test Tenant", TaxNumber = "1234567890", IsActive = true };
            // Set specific Id via reflection
            typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, TestTenantId);
            _context.Set<Tenant>().Add(tenant);
            await _context.SaveChangesAsync();
        }

        // Create a category for each test run (unique Id)
        var category = new Category
        {
            Name = "Test Kategori",
            Code = $"TST-{Guid.NewGuid().ToString()[..8]}",
            TenantId = TestTenantId,
            IsActive = true
        };
        _context.Set<Category>().Add(category);
        await _context.SaveChangesAsync();
        _categoryId = category.Id;

        _productRepo = new ProductRepository(_context);
        _movementRepo = new StockMovementRepository(_context);
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        _context.StockMovements.RemoveRange(_context.StockMovements.IgnoreQueryFilters());
        await _context.SaveChangesAsync();
        _context.Products.RemoveRange(_context.Products.IgnoreQueryFilters());
        await _context.SaveChangesAsync();
        _context.Set<Category>().RemoveRange(
            _context.Set<Category>().IgnoreQueryFilters().Where(c => c.Id == _categoryId));
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task ProductCrud_ShouldWorkWithRealPostgres()
    {
        var product = new Product
        {
            Name = "PostgreSQL Test Urun",
            SKU = "PG-CRUD-001",
            Barcode = "8690000000001",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Stock = 25,
            CategoryId = _categoryId,
            TenantId = TestTenantId,
            IsActive = true
        };
        await _productRepo.AddAsync(product);
        await _context.SaveChangesAsync();

        var saved = await _productRepo.GetByIdAsync(product.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("PostgreSQL Test Urun");
        saved.Stock.Should().Be(25);

        saved.Name = "Updated Name";
        saved.SalePrice = 120m;
        await _productRepo.UpdateAsync(saved);
        await _context.SaveChangesAsync();

        var updated = await _productRepo.GetByIdAsync(product.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.SalePrice.Should().Be(120m);

        await _productRepo.DeleteAsync(product.Id);
        await _context.SaveChangesAsync();

        var deleted = await _productRepo.GetByIdAsync(product.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WithILike_ShouldWorkOnRealPostgres()
    {
        _context.Products.AddRange(
            new Product { Name = "Samsung Galaxy S24", SKU = "SAM-S24", CategoryId = _categoryId, TenantId = TestTenantId, IsActive = true },
            new Product { Name = "Apple iPhone 16", SKU = "APL-IP16", CategoryId = _categoryId, TenantId = TestTenantId, IsActive = true },
            new Product { Name = "Samsung Tab A9", SKU = "SAM-TA9", CategoryId = _categoryId, TenantId = TestTenantId, IsActive = true }
        );
        await _context.SaveChangesAsync();

        var results = await _productRepo.SearchAsync("samsung");

        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.Name.Contains("Samsung"));
    }

    [Fact]
    public async Task SearchAsync_BySku_ShouldFindProduct()
    {
        _context.Products.Add(new Product
        {
            Name = "Barkod Urun",
            SKU = "SRCH-SKU-001",
            Barcode = "8690000000099",
            CategoryId = _categoryId,
            TenantId = TestTenantId,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var results = await _productRepo.SearchAsync("SRCH-SKU");

        results.Should().ContainSingle();
        results.First().Barcode.Should().Be("8690000000099");
    }

    [Fact]
    public async Task StockMovement_FullStack_ShouldPersistAndRetrieve()
    {
        var product = new Product
        {
            Name = "Movement Test",
            SKU = "PG-MOV-001",
            Stock = 100,
            CategoryId = _categoryId,
            TenantId = TestTenantId
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = -20,
            MovementType = StockMovementType.Sale.ToString(),
            Reason = "Satis",
            TenantId = TestTenantId,
            Date = DateTime.UtcNow
        };
        movement.SetStockLevels(100, 80);
        await _movementRepo.AddAsync(movement);
        await _context.SaveChangesAsync();

        var retrieved = await _movementRepo.GetByProductIdAsync(product.Id);

        retrieved.Should().ContainSingle();
        retrieved.First().Quantity.Should().Be(-20);
        retrieved.First().Reason.Should().Be("Satis");
    }

    [Fact]
    public async Task ConcurrentInserts_ShouldNotLoseData()
    {
        var catId = _categoryId;
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await using var ctx = new AppDbContext(CreateOptions(), new TestTenantProvider());
            ctx.Products.Add(new Product
            {
                Name = $"Concurrent-{i}",
                SKU = $"CONC-{i:D3}",
                CategoryId = catId,
                TenantId = TestTenantId,
                IsActive = true
            });
            await ctx.SaveChangesAsync();
        });

        await Task.WhenAll(tasks);

        var count = await _context.Products.IgnoreQueryFilters()
            .CountAsync(p => p.SKU.StartsWith("CONC-"));

        count.Should().Be(5, "all 5 concurrent inserts should succeed");
    }

    private sealed class TestTenantProvider : ITenantProvider
    {
        public Guid GetCurrentTenantId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
