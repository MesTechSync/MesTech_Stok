using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Integration._Shared;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// Tenant izolasyonu testleri — KURSUN GECIRMEZ.
/// Bu testler kirilirsa = tenant izolasyonu bozulmus demektir.
///
/// EF Core global query filter ile tenant izolasyonu test edilir.
/// Context TenantId=1 ile olusturulur, filtre otomatik uygulanir.
/// IgnoreQueryFilters() ile admin erisimi simule edilir.
/// </summary>
public class GlobalQueryFilterTests : IntegrationTestBase
{
    [Fact]
    public async Task TenantA_ShouldNotSee_TenantB_Products()
    {
        // Arrange — IgnoreQueryFilters ile veri ekle
        var productA = new Product
        {
            Name = "Urun A",
            SKU = "SKU-A-001",
            TenantId = 1,  // Context tenant ID ile eslesir
            CategoryId = 1,
            PurchasePrice = 50,
            SalePrice = 100
        };

        var productB = new Product
        {
            Name = "Urun B",
            SKU = "SKU-B-001",
            TenantId = 2,  // Farkli tenant
            CategoryId = 1,
            PurchasePrice = 60,
            SalePrice = 120
        };

        Context.Products.AddRange(productA, productB);
        await Context.SaveChangesAsync();

        // Act — Tenant 1 olarak sorgula (global filter aktif)
        var results = await Context.Products.ToListAsync();

        // Assert — Tenant 1 sadece kendi urunlerini gorur
        results.Should().ContainSingle();
        results.First().Name.Should().Be("Urun A");
        results.Should().NotContain(p => p.Name == "Urun B");
    }

    [Fact]
    public async Task TenantA_ShouldNotModify_TenantB_Products()
    {
        // Arrange
        var productB = new Product
        {
            Name = "Urun B Orijinal",
            SKU = "SKU-B-MOD",
            TenantId = 2,
            CategoryId = 1,
            PurchasePrice = 50,
            SalePrice = 100
        };

        Context.Products.Add(productB);
        await Context.SaveChangesAsync();

        // Act — Tenant 1 olarak Tenant 2'nin urununu sorgula (filter uygulanir)
        var filteredProducts = await Context.Products.ToListAsync();

        // Assert — Tenant 1, Tenant 2'nin urununu goremez
        filteredProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task GlobalAdmin_ShouldSee_AllTenants()
    {
        // Arrange
        Context.Products.AddRange(
            new Product { Name = "Urun T1", SKU = "SKU-T1", TenantId = 1, CategoryId = 1 },
            new Product { Name = "Urun T2", SKU = "SKU-T2", TenantId = 2, CategoryId = 1 },
            new Product { Name = "Urun T3", SKU = "SKU-T3", TenantId = 3, CategoryId = 1 }
        );
        await Context.SaveChangesAsync();

        // Act — Global admin filtre bypass: IgnoreQueryFilters
        var allProducts = await Context.Products.IgnoreQueryFilters().ToListAsync();

        // Assert
        allProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task TenantSwitch_ShouldFilterCorrectly()
    {
        // Arrange — IgnoreQueryFilters ile veri ekle
        Context.Products.AddRange(
            new Product { Name = "A1", SKU = "SKU-SW-A1", TenantId = 1, CategoryId = 1 },
            new Product { Name = "A2", SKU = "SKU-SW-A2", TenantId = 1, CategoryId = 1 },
            new Product { Name = "B1", SKU = "SKU-SW-B1", TenantId = 2, CategoryId = 1 }
        );
        await Context.SaveChangesAsync();

        // Act & Assert — Context TenantId=1 ile sadece Tenant 1 urunleri
        var tenant1Products = await Context.Products.ToListAsync();
        tenant1Products.Should().HaveCount(2);
        tenant1Products.Should().OnlyContain(p => p.TenantId == 1);

        // Act & Assert — IgnoreQueryFilters ile tum urunler
        var allProducts = await Context.Products.IgnoreQueryFilters()
            .Where(p => !p.IsDeleted)
            .ToListAsync();
        allProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task SoftDelete_ShouldFilterDeletedEntities()
    {
        // Arrange
        var activeProduct = new Product
        {
            Name = "Aktif Urun",
            SKU = "SKU-ACTIVE",
            TenantId = 1,
            CategoryId = 1,
            IsDeleted = false
        };

        var deletedProduct = new Product
        {
            Name = "Silinen Urun",
            SKU = "SKU-DELETED",
            TenantId = 1,
            CategoryId = 1,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "admin"
        };

        Context.Products.AddRange(activeProduct, deletedProduct);
        await Context.SaveChangesAsync();

        // Act — soft-delete filter simule et
        SetCurrentTenant(1);
        var results = await ApplyTenantFilter(Context.Products.AsQueryable())
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        // Assert
        results.Should().ContainSingle();
        results.First().Name.Should().Be("Aktif Urun");
    }

    [Fact]
    public async Task StoreEntity_ShouldFilterByTenant()
    {
        // Arrange
        Context.Stores.AddRange(
            new Store { StoreName = "Trendyol T1", TenantId = 1, PlatformType = Domain.Enums.PlatformType.Trendyol },
            new Store { StoreName = "Trendyol T2", TenantId = 2, PlatformType = Domain.Enums.PlatformType.Trendyol }
        );
        await Context.SaveChangesAsync();

        // Act
        SetCurrentTenant(1);
        var stores = await ApplyTenantFilter(Context.Stores.AsQueryable()).ToListAsync();

        // Assert
        stores.Should().ContainSingle();
        stores.First().StoreName.Should().Be("Trendyol T1");
    }
}
