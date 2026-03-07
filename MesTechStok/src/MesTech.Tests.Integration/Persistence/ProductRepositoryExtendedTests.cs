using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Tests.Integration._Shared;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// Extended ProductRepository tests — covers GetByBarcode, GetAll (IsActive filter),
/// GetByCategory, GetCount, and Delete-nonexistent edge case.
/// SearchAsync uses EF.Functions.ILike (PostgreSQL-only) — tested in Testcontainers suite.
/// </summary>
[Trait("Category", "Integration")]
public class ProductRepositoryExtendedTests : IntegrationTestBase
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CatElectronics = Guid.NewGuid();
    private static readonly Guid CatClothing = Guid.NewGuid();
    private readonly ProductRepository _repo;

    public ProductRepositoryExtendedTests()
    {
        _repo = new ProductRepository(Context);
    }

    // ── GetByBarcodeAsync ──

    [Fact]
    public async Task GetByBarcodeAsync_ExistingBarcode_ShouldReturnProduct()
    {
        Context.Products.Add(new Product
        {
            Name = "Barcode Urun",
            SKU = "BAR-001",
            Barcode = "8691234567890",
            CategoryId = CatElectronics,
            TenantId = TestTenantId
        });
        await Context.SaveChangesAsync();

        var result = await _repo.GetByBarcodeAsync("8691234567890");

        result.Should().NotBeNull();
        result!.SKU.Should().Be("BAR-001");
        result.Name.Should().Be("Barcode Urun");
    }

    [Fact]
    public async Task GetByBarcodeAsync_NonExistent_ShouldReturnNull()
    {
        var result = await _repo.GetByBarcodeAsync("0000000000000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByBarcodeAsync_NullBarcode_ShouldReturnNull()
    {
        Context.Products.Add(new Product
        {
            Name = "No Barcode",
            SKU = "NB-001",
            Barcode = null,
            CategoryId = CatElectronics,
            TenantId = TestTenantId
        });
        await Context.SaveChangesAsync();

        var result = await _repo.GetByBarcodeAsync("8691234567890");

        result.Should().BeNull();
    }

    // ── GetAllAsync (IsActive filter) ──

    [Fact]
    public async Task GetAllAsync_ShouldOnlyReturnActiveProducts()
    {
        Context.Products.AddRange(
            new Product { Name = "Active1", SKU = "ACT-001", IsActive = true, CategoryId = CatElectronics, TenantId = TestTenantId },
            new Product { Name = "Active2", SKU = "ACT-002", IsActive = true, CategoryId = CatElectronics, TenantId = TestTenantId },
            new Product { Name = "Inactive", SKU = "INA-001", IsActive = false, CategoryId = CatElectronics, TenantId = TestTenantId }
        );
        await Context.SaveChangesAsync();

        var results = await _repo.GetAllAsync();

        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var results = await _repo.GetAllAsync();

        results.Should().BeEmpty();
    }

    // ── GetByCategoryAsync ──

    [Fact]
    public async Task GetByCategoryAsync_ShouldFilterByCategory()
    {
        Context.Products.AddRange(
            new Product { Name = "Laptop", SKU = "ELC-001", CategoryId = CatElectronics, TenantId = TestTenantId, IsActive = true },
            new Product { Name = "Telefon", SKU = "ELC-002", CategoryId = CatElectronics, TenantId = TestTenantId, IsActive = true },
            new Product { Name = "Tisort", SKU = "CLT-001", CategoryId = CatClothing, TenantId = TestTenantId, IsActive = true }
        );
        await Context.SaveChangesAsync();

        var electronics = await _repo.GetByCategoryAsync(CatElectronics);
        var clothing = await _repo.GetByCategoryAsync(CatClothing);

        electronics.Should().HaveCount(2);
        electronics.Should().OnlyContain(p => p.CategoryId == CatElectronics);
        clothing.Should().ContainSingle();
        clothing.First().Name.Should().Be("Tisort");
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldExcludeInactiveProducts()
    {
        Context.Products.AddRange(
            new Product { Name = "Active", SKU = "CATACT-001", CategoryId = CatElectronics, TenantId = TestTenantId, IsActive = true },
            new Product { Name = "Inactive", SKU = "CATINA-001", CategoryId = CatElectronics, TenantId = TestTenantId, IsActive = false }
        );
        await Context.SaveChangesAsync();

        var results = await _repo.GetByCategoryAsync(CatElectronics);

        results.Should().ContainSingle();
        results.First().Name.Should().Be("Active");
    }

    // ── GetCountAsync ──

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        Context.Products.AddRange(
            new Product { Name = "P1", SKU = "CNT-001", CategoryId = CatElectronics, TenantId = TestTenantId },
            new Product { Name = "P2", SKU = "CNT-002", CategoryId = CatElectronics, TenantId = TestTenantId },
            new Product { Name = "P3", SKU = "CNT-003", CategoryId = CatElectronics, TenantId = TestTenantId }
        );
        await Context.SaveChangesAsync();

        var count = await _repo.GetCountAsync();

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetCountAsync_WhenEmpty_ShouldReturnZero()
    {
        var count = await _repo.GetCountAsync();

        count.Should().Be(0);
    }

    // ── DeleteAsync edge case ──

    [Fact]
    public async Task DeleteAsync_NonExistentId_ShouldNotThrow()
    {
        var act = async () =>
        {
            await _repo.DeleteAsync(Guid.NewGuid());
            await Context.SaveChangesAsync();
        };

        await act.Should().NotThrowAsync();
    }
}
