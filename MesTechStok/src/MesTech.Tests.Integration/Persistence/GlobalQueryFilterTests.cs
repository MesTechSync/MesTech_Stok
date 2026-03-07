using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Integration._Shared;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// Tenant izolasyonu testleri — KURSUN GECIRMEZ.
/// Bu testler kirilirsa = tenant izolasyonu bozulmus demektir.
/// </summary>
[Trait("Category", "Integration")]
public class GlobalQueryFilterTests : IntegrationTestBase
{
    private static readonly Guid TenantA = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid TenantC = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid DefaultCategoryId = Guid.NewGuid();

    [Fact]
    public async Task TenantA_ShouldNotSee_TenantB_Products()
    {
        var productA = new Product
        {
            Name = "Urun A", SKU = "SKU-A-001",
            TenantId = TenantA, CategoryId = DefaultCategoryId,
            PurchasePrice = 50, SalePrice = 100
        };
        var productB = new Product
        {
            Name = "Urun B", SKU = "SKU-B-001",
            TenantId = TenantB, CategoryId = DefaultCategoryId,
            PurchasePrice = 60, SalePrice = 120
        };

        Context.Products.AddRange(productA, productB);
        await Context.SaveChangesAsync();

        // Tenant A olarak sorgula
        SetCurrentTenant(TenantA);
        var results = await ApplyTenantFilter(Context.Products.AsQueryable()).ToListAsync();

        results.Should().ContainSingle();
        results.First().Name.Should().Be("Urun A");
    }

    [Fact]
    public async Task TenantA_ShouldNotModify_TenantB_Products()
    {
        var productB = new Product
        {
            Name = "Urun B Orijinal", SKU = "SKU-B-MOD",
            TenantId = TenantB, CategoryId = DefaultCategoryId,
            PurchasePrice = 50, SalePrice = 100
        };

        Context.Products.Add(productB);
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var filteredProducts = await ApplyTenantFilter(Context.Products.AsQueryable()).ToListAsync();

        filteredProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task GlobalAdmin_ShouldSee_AllTenants()
    {
        Context.Products.AddRange(
            new Product { Name = "Urun T1", SKU = "SKU-T1", TenantId = TenantA, CategoryId = DefaultCategoryId },
            new Product { Name = "Urun T2", SKU = "SKU-T2", TenantId = TenantB, CategoryId = DefaultCategoryId },
            new Product { Name = "Urun T3", SKU = "SKU-T3", TenantId = TenantC, CategoryId = DefaultCategoryId }
        );
        await Context.SaveChangesAsync();

        var allProducts = await Context.Products.IgnoreQueryFilters().ToListAsync();

        allProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task TenantSwitch_ShouldFilterCorrectly()
    {
        Context.Products.AddRange(
            new Product { Name = "A1", SKU = "SKU-SW-A1", TenantId = TenantA, CategoryId = DefaultCategoryId },
            new Product { Name = "A2", SKU = "SKU-SW-A2", TenantId = TenantA, CategoryId = DefaultCategoryId },
            new Product { Name = "B1", SKU = "SKU-SW-B1", TenantId = TenantB, CategoryId = DefaultCategoryId }
        );
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var tenant1Products = await ApplyTenantFilter(Context.Products.AsQueryable()).ToListAsync();
        tenant1Products.Should().HaveCount(2);
        tenant1Products.Should().OnlyContain(p => p.TenantId == TenantA);

        var allProducts = await Context.Products.IgnoreQueryFilters()
            .Where(p => !p.IsDeleted).ToListAsync();
        allProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task SoftDelete_ShouldFilterDeletedEntities()
    {
        var activeProduct = new Product
        {
            Name = "Aktif Urun", SKU = "SKU-ACTIVE",
            TenantId = TenantA, CategoryId = DefaultCategoryId, IsDeleted = false
        };
        var deletedProduct = new Product
        {
            Name = "Silinen Urun", SKU = "SKU-DELETED",
            TenantId = TenantA, CategoryId = DefaultCategoryId,
            IsDeleted = true, DeletedAt = DateTime.UtcNow, DeletedBy = "admin"
        };

        Context.Products.AddRange(activeProduct, deletedProduct);
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var results = await ApplyTenantFilter(Context.Products.AsQueryable())
            .Where(p => !p.IsDeleted).ToListAsync();

        results.Should().ContainSingle();
        results.First().Name.Should().Be("Aktif Urun");
    }

    [Fact]
    public async Task StoreEntity_ShouldFilterByTenant()
    {
        Context.Stores.AddRange(
            new Store { StoreName = "Trendyol T1", TenantId = TenantA, PlatformType = Domain.Enums.PlatformType.Trendyol },
            new Store { StoreName = "Trendyol T2", TenantId = TenantB, PlatformType = Domain.Enums.PlatformType.Trendyol }
        );
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var stores = await ApplyTenantFilter(Context.Stores.AsQueryable()).ToListAsync();

        stores.Should().ContainSingle();
        stores.First().StoreName.Should().Be("Trendyol T1");
    }

    [Fact]
    public async Task MultipleTenants_ShouldHaveCompleteIsolation()
    {
        var tA = Guid.NewGuid();
        var tB = Guid.NewGuid();
        var tC = Guid.NewGuid();

        Context.Products.AddRange(
            new Product { Name = "A-Urun1", SKU = "ISO-A1", TenantId = tA, CategoryId = DefaultCategoryId },
            new Product { Name = "A-Urun2", SKU = "ISO-A2", TenantId = tA, CategoryId = DefaultCategoryId },
            new Product { Name = "B-Urun1", SKU = "ISO-B1", TenantId = tB, CategoryId = DefaultCategoryId },
            new Product { Name = "C-Urun1", SKU = "ISO-C1", TenantId = tC, CategoryId = DefaultCategoryId },
            new Product { Name = "C-Urun2", SKU = "ISO-C2", TenantId = tC, CategoryId = DefaultCategoryId },
            new Product { Name = "C-Urun3", SKU = "ISO-C3", TenantId = tC, CategoryId = DefaultCategoryId }
        );
        await Context.SaveChangesAsync();

        SetCurrentTenant(tA);
        var productsA = await ApplyTenantFilter(Context.Products.AsQueryable()).ToListAsync();
        productsA.Should().HaveCount(2);
        productsA.Should().OnlyContain(p => p.TenantId == tA);

        SetCurrentTenant(tB);
        var productsB = await ApplyTenantFilter(Context.Products.AsQueryable()).ToListAsync();
        productsB.Should().HaveCount(1);
        productsB.First().Name.Should().Be("B-Urun1");

        SetCurrentTenant(tC);
        var productsC = await ApplyTenantFilter(Context.Products.AsQueryable()).ToListAsync();
        productsC.Should().HaveCount(3);
        productsC.Should().OnlyContain(p => p.TenantId == tC);

        var allProducts = await Context.Products.IgnoreQueryFilters()
            .Where(p => !p.IsDeleted).ToListAsync();
        allProducts.Should().HaveCount(6);
    }

    [Fact]
    public async Task TenantIsolation_ShouldApplyToStockMovements()
    {
        var productT1 = new Product { Name = "SM-T1", SKU = "SM-T1-001", TenantId = TenantA, CategoryId = DefaultCategoryId };
        var productT2 = new Product { Name = "SM-T2", SKU = "SM-T2-001", TenantId = TenantB, CategoryId = DefaultCategoryId };
        Context.Products.AddRange(productT1, productT2);
        await Context.SaveChangesAsync();

        Context.StockMovements.AddRange(
            new StockMovement
            {
                ProductId = productT1.Id, Quantity = 50,
                TenantId = TenantA, Reason = "Test giris"
            },
            new StockMovement
            {
                ProductId = productT2.Id, Quantity = 100,
                TenantId = TenantB, Reason = "Test giris"
            }
        );
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var movements = await ApplyTenantFilter(
            Context.StockMovements.AsQueryable()).ToListAsync();

        movements.Should().ContainSingle();
        movements.First().TenantId.Should().Be(TenantA);
        movements.First().Quantity.Should().Be(50);
    }

    [Fact]
    public async Task SoftDelete_CombinedWithTenant_ShouldFilterBoth()
    {
        Context.Products.AddRange(
            new Product { Name = "T1-Aktif", SKU = "SD-T1-ACT", TenantId = TenantA, CategoryId = DefaultCategoryId, IsDeleted = false },
            new Product { Name = "T1-Silinen", SKU = "SD-T1-DEL", TenantId = TenantA, CategoryId = DefaultCategoryId, IsDeleted = true, DeletedAt = DateTime.UtcNow },
            new Product { Name = "T2-Aktif", SKU = "SD-T2-ACT", TenantId = TenantB, CategoryId = DefaultCategoryId, IsDeleted = false }
        );
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var results = await ApplyTenantFilter(Context.Products.AsQueryable())
            .Where(p => !p.IsDeleted).ToListAsync();

        results.Should().ContainSingle();
        results.First().Name.Should().Be("T1-Aktif");

        var allIncludingDeleted = await Context.Products.IgnoreQueryFilters().ToListAsync();
        allIncludingDeleted.Should().HaveCount(3);
    }

    [Fact]
    public async Task TenantIsolation_CountQueries_ShouldRespectFilter()
    {
        Context.Products.AddRange(
            new Product { Name = "Count-T1-1", SKU = "CNT-T1-1", TenantId = TenantA, CategoryId = DefaultCategoryId },
            new Product { Name = "Count-T1-2", SKU = "CNT-T1-2", TenantId = TenantA, CategoryId = DefaultCategoryId },
            new Product { Name = "Count-T2-1", SKU = "CNT-T2-1", TenantId = TenantB, CategoryId = DefaultCategoryId },
            new Product { Name = "Count-T2-2", SKU = "CNT-T2-2", TenantId = TenantB, CategoryId = DefaultCategoryId },
            new Product { Name = "Count-T2-3", SKU = "CNT-T2-3", TenantId = TenantB, CategoryId = DefaultCategoryId }
        );
        await Context.SaveChangesAsync();

        SetCurrentTenant(TenantA);
        var countT1 = await ApplyTenantFilter(Context.Products.AsQueryable()).CountAsync();

        SetCurrentTenant(TenantB);
        var countT2 = await ApplyTenantFilter(Context.Products.AsQueryable()).CountAsync();

        countT1.Should().Be(2);
        countT2.Should().Be(3);
    }
}
