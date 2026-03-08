using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// Multi-Tenant Global Query Filter izolasyon testleri.
/// Kanıt: Tenant A verisi Tenant B'den görünmez, admin bypass çalışır.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class MultiTenantQueryFilterTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly string _dbName = $"MultiTenantTest_{Guid.NewGuid()}";

    private AppDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        var tenantProvider = new StubTenantProvider(tenantId);

        return new AppDbContext(options, tenantProvider);
    }

    // ── Test 1: Tenant A ürün ekler → Tenant B göremez ──

    [Fact]
    public async Task TenantA_Products_ShouldNotBeVisibleTo_TenantB()
    {
        // Arrange — Tenant A ürün ekler
        using (var ctxA = CreateContext(TenantA))
        {
            ctxA.Products.Add(new Product
            {
                Name = "A-Widget",
                SKU = "SKU-A-001",
                TenantId = TenantA,
                PurchasePrice = 10m,
                SalePrice = 20m,
                CategoryId = Guid.NewGuid(),
            });
            await ctxA.SaveChangesAsync();
        }

        // Act — Tenant B sorgular
        using var ctxB = CreateContext(TenantB);
        var productsB = await ctxB.Products.ToListAsync();

        // Assert — Tenant B hiçbir ürün görmez
        productsB.Should().BeEmpty("Tenant B should not see Tenant A's products");
    }

    // ── Test 2: Her tenant sadece kendi verisini görür ──

    [Fact]
    public async Task EachTenant_ShouldOnlySee_OwnProducts()
    {
        // Arrange — her iki tenant'a ürün ekle
        using (var ctxSeed = CreateContext(TenantA))
        {
            ctxSeed.Products.AddRange(
                new Product { Name = "A-Alpha", SKU = "SKU-A-100", TenantId = TenantA, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() },
                new Product { Name = "A-Beta", SKU = "SKU-A-101", TenantId = TenantA, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() },
                new Product { Name = "B-Gamma", SKU = "SKU-B-200", TenantId = TenantB, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() }
            );
            await ctxSeed.SaveChangesAsync();
        }

        // Act
        using var ctxA = CreateContext(TenantA);
        using var ctxB = CreateContext(TenantB);

        var listA = await ctxA.Products.ToListAsync();
        var listB = await ctxB.Products.ToListAsync();

        // Assert
        listA.Should().HaveCount(2);
        listA.Should().AllSatisfy(p => p.TenantId.Should().Be(TenantA));

        listB.Should().HaveCount(1);
        listB.Single().Name.Should().Be("B-Gamma");
    }

    // ── Test 3: IgnoreQueryFilters() ile admin tüm veriyi görebilir ──

    [Fact]
    public async Task Admin_WithIgnoreQueryFilters_ShouldSeeAllTenants()
    {
        // Arrange
        using (var ctxSeed = CreateContext(TenantA))
        {
            ctxSeed.Products.AddRange(
                new Product { Name = "A-Product", SKU = "SKU-A-ADM", TenantId = TenantA, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() },
                new Product { Name = "B-Product", SKU = "SKU-B-ADM", TenantId = TenantB, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() }
            );
            await ctxSeed.SaveChangesAsync();
        }

        // Act — herhangi bir tenant context'i ile IgnoreQueryFilters çağır
        using var ctx = CreateContext(TenantA);
        var allProducts = await ctx.Products.IgnoreQueryFilters().ToListAsync();

        // Assert — her iki tenant'ın ürünleri de görünür
        allProducts.Should().HaveCount(2);
        allProducts.Select(p => p.TenantId).Distinct().Should().HaveCount(2,
            "admin bypass should reveal products from both tenants");
    }

    // ── Test 4: SyncLog tenant filter'dan muaf (platform-agnostic) ──

    [Fact]
    public async Task SyncLog_ShouldBeVisibleToAllTenants_WithoutBypass()
    {
        // Arrange — TenantA context'i ile SyncLog ekle (TenantB ownershipı ile)
        using (var ctxSeed = CreateContext(TenantA))
        {
            ctxSeed.SyncLogs.Add(new SyncLog
            {
                TenantId = TenantB, // farklı tenant'ın log'u
                PlatformCode = "Trendyol",
                Direction = SyncDirection.Push,
                EntityType = "Product",
                IsSuccess = true,
            });
            await ctxSeed.SaveChangesAsync();
        }

        // Act — TenantA context'i ile sorgula (IgnoreQueryFilters yok!)
        using var ctxA = CreateContext(TenantA);
        var logs = await ctxA.SyncLogs.ToListAsync();

        // Assert — SyncLog tenant filter'dan muaf, TenantA bile TenantB'nin log'unu görür
        logs.Should().HaveCount(1);
        logs.Single().TenantId.Should().Be(TenantB);
    }

    // ── Test 5: Soft-delete filter tenant filter ile birlikte çalışır ──

    [Fact]
    public async Task SoftDeletedProducts_ShouldNotBeVisible_EvenForCorrectTenant()
    {
        // Arrange
        using (var ctxSeed = CreateContext(TenantA))
        {
            ctxSeed.Products.AddRange(
                new Product { Name = "Active", SKU = "SKU-ACT", TenantId = TenantA, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid() },
                new Product { Name = "Deleted", SKU = "SKU-DEL", TenantId = TenantA, PurchasePrice = 5m, SalePrice = 10m, CategoryId = Guid.NewGuid(), IsDeleted = true, DeletedAt = DateTime.UtcNow }
            );
            await ctxSeed.SaveChangesAsync();
        }

        // Act
        using var ctx = CreateContext(TenantA);
        var products = await ctx.Products.ToListAsync();

        // Assert — soft-deleted ürün görünmez
        products.Should().HaveCount(1);
        products.Single().Name.Should().Be("Active");
    }

    public void Dispose()
    {
        // InMemory database, GC ile temizlenir — explicit dispose gerekmez.
        GC.SuppressFinalize(this);
    }

    // ── Stub ITenantProvider ──

    private class StubTenantProvider : ITenantProvider
    {
        private readonly Guid _tenantId;
        public StubTenantProvider(Guid tenantId) => _tenantId = tenantId;
        public Guid GetCurrentTenantId() => _tenantId;
    }
}
