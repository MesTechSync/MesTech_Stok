using FluentAssertions;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Security;

/// <summary>
/// G029: Tenant isolation integration test.
/// Cross-tenant data leak kontrolü — FindAsync→FirstOrDefault migration doğrulaması.
/// EF Core InMemory ile 2 tenant senaryosu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Security")]
[Trait("Group", "TenantIsolation")]
public class TenantIsolationTests : IDisposable
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly TestTenantProvider _tenantProvider;
    private readonly AppDbContext _context;

    public TenantIsolationTests()
    {
        _tenantProvider = new TestTenantProvider(_tenantA);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TenantIsolation_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options, _tenantProvider);

        // Seed: her tenant'a 3 ürün
        SeedProducts();
    }

    private void SeedProducts()
    {
        // Tenant A ürünleri
        _tenantProvider.SetTenant(_tenantA);
        for (int i = 1; i <= 3; i++)
        {
            _context.Products.Add(new Product
            {
                TenantId = _tenantA,
                Name = $"A-Product-{i}",
                SKU = $"SKU-A-{i}",
                PurchasePrice = 50m,
                SalePrice = 100m,
                CategoryId = Guid.NewGuid()
            });
        }

        // Tenant B ürünleri
        for (int i = 1; i <= 3; i++)
        {
            _context.Products.Add(new Product
            {
                TenantId = _tenantB,
                Name = $"B-Product-{i}",
                SKU = $"SKU-B-{i}",
                PurchasePrice = 60m,
                SalePrice = 120m,
                CategoryId = Guid.NewGuid()
            });
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task TenantA_ShouldOnly_See_OwnProducts()
    {
        _tenantProvider.SetTenant(_tenantA);
        var products = await _context.Products.ToListAsync();

        products.Should().HaveCount(3);
        products.Should().OnlyContain(p => p.TenantId == _tenantA);
        products.Should().OnlyContain(p => p.Name.StartsWith("A-"));
    }

    [Fact]
    public async Task TenantB_ShouldOnly_See_OwnProducts()
    {
        _tenantProvider.SetTenant(_tenantB);
        var products = await _context.Products.ToListAsync();

        products.Should().HaveCount(3);
        products.Should().OnlyContain(p => p.TenantId == _tenantB);
        products.Should().OnlyContain(p => p.Name.StartsWith("B-"));
    }

    [Fact]
    public async Task TenantA_ShouldNot_See_TenantB_Products()
    {
        _tenantProvider.SetTenant(_tenantA);
        var tenantBProducts = await _context.Products
            .Where(p => p.TenantId == _tenantB)
            .ToListAsync();

        // Global query filter TenantB ürünlerini gizlemeli
        tenantBProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldSee_AllTenants()
    {
        _tenantProvider.SetTenant(_tenantA);
        var allProducts = await _context.Products
            .IgnoreQueryFilters()
            .ToListAsync();

        // Admin görünümü — tüm tenant ürünleri
        allProducts.Should().HaveCount(6); // 3A + 3B
    }

    [Fact]
    public async Task Count_ShouldRespect_TenantFilter()
    {
        _tenantProvider.SetTenant(_tenantA);
        var count = await _context.Products.CountAsync();

        count.Should().Be(3); // sadece TenantA
    }

    [Fact]
    public async Task FirstOrDefault_ShouldRespect_TenantFilter()
    {
        // TenantB'nin SKU'su ile TenantA context'inde arama
        _tenantProvider.SetTenant(_tenantA);
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == "SKU-B-1");

        // TenantA context'inde TenantB ürünü bulunmamalı
        product.Should().BeNull();
    }

    [Fact]
    public async Task TenantSwitch_ShouldFilter_Correctly()
    {
        // İlk tenant
        _tenantProvider.SetTenant(_tenantA);
        var countA = await _context.Products.CountAsync();

        // Switch to tenant B
        _tenantProvider.SetTenant(_tenantB);
        var countB = await _context.Products.CountAsync();

        countA.Should().Be(3);
        countB.Should().Be(3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>Test tenant provider — SetTenant ile dinamik geçiş.</summary>
    private sealed class TestTenantProvider : ITenantProvider
    {
        private Guid _tenantId;
        public TestTenantProvider(Guid initialTenantId) => _tenantId = initialTenantId;
        public Guid GetCurrentTenantId() => _tenantId;
        public void SetTenant(Guid tenantId) => _tenantId = tenantId;
    }
}
