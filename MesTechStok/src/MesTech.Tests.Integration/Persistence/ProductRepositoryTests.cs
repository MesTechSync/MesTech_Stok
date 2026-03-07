using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Tests.Integration._Shared;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// Product repository integration testleri (InMemory DB).
/// </summary>
[Trait("Category", "Integration")]
public class ProductRepositoryTests : IntegrationTestBase
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestCategoryId = Guid.NewGuid();
    private readonly ProductRepository _repo;

    public ProductRepositoryTests()
    {
        _repo = new ProductRepository(Context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistProduct()
    {
        var product = new Product
        {
            Name = "Test Urun",
            SKU = "TST-001",
            Barcode = "8691234567005",
            PurchasePrice = 50,
            SalePrice = 100,
            Stock = 25,
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        };

        await _repo.AddAsync(product);
        await Context.SaveChangesAsync();

        var saved = await Context.Products.FirstAsync(p => p.SKU == "TST-001");
        saved.Should().NotBeNull();
        saved.Name.Should().Be("Test Urun");
        saved.Stock.Should().Be(25);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct()
    {
        var product = new Product
        {
            Name = "ById Test",
            SKU = "BID-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("ById Test");
    }

    [Fact]
    public async Task GetBySKUAsync_ShouldReturnProduct()
    {
        Context.Products.Add(new Product
        {
            Name = "SKU Test",
            SKU = "SKUT-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        });
        await Context.SaveChangesAsync();

        var result = await _repo.GetBySKUAsync("SKUT-001");

        result.Should().NotBeNull();
        result!.Name.Should().Be("SKU Test");
    }

    [Fact]
    public async Task GetBySKUAsync_NonExistent_ShouldReturnNull()
    {
        var result = await _repo.GetBySKUAsync("NONEXISTENT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var product = new Product
        {
            Name = "Before Update",
            SKU = "UPD-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId,
            SalePrice = 100
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        product.Name = "After Update";
        product.SalePrice = 150;
        await _repo.UpdateAsync(product);
        await Context.SaveChangesAsync();

        var updated = await Context.Products.FindAsync(product.Id);
        updated!.Name.Should().Be("After Update");
        updated.SalePrice.Should().Be(150);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct()
    {
        var product = new Product
        {
            Name = "Delete Test",
            SKU = "DEL-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        var productId = product.Id;

        await _repo.DeleteAsync(productId);
        await Context.SaveChangesAsync();

        var deleted = await Context.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == productId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetLowStockAsync_ShouldReturnLowStockProducts()
    {
        Context.Products.AddRange(
            new Product { Name = "Low", SKU = "LOW-001", Stock = 2, MinimumStock = 5, CategoryId = TestCategoryId, TenantId = TestTenantId },
            new Product { Name = "Normal", SKU = "NOR-001", Stock = 50, MinimumStock = 5, CategoryId = TestCategoryId, TenantId = TestTenantId }
        );
        await Context.SaveChangesAsync();

        var results = await _repo.GetLowStockAsync();

        results.Should().ContainSingle();
        results.First().Name.Should().Be("Low");
    }
}
