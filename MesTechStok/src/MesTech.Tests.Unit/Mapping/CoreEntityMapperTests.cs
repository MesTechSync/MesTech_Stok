using MesTechStok.Desktop.Mapping;
using Xunit;

namespace MesTech.Tests.Unit.Mapping;

public class CoreEntityMapperTests
{
    private readonly CoreEntityMapper _mapper = new();

    // ── ToDomainProduct ──

    [Fact]
    public void ToDomainProduct_MapsBasicProperties()
    {
#pragma warning disable CS0618
        var coreProduct = new MesTechStok.Core.Data.Models.Product
        {
            Name = "Test Product",
            SKU = "SKU-001",
            SalePrice = 100.50m,
            Stock = 25,
            Description = "Test description"
        };
#pragma warning restore CS0618

        var domain = _mapper.ToDomainProduct(coreProduct);

        Assert.Equal("Test Product", domain.Name);
        Assert.Equal("SKU-001", domain.SKU);
        Assert.Equal(100.50m, domain.SalePrice);
        Assert.Equal(25, domain.Stock);
        Assert.Equal("Test description", domain.Description);
    }

    [Fact]
    public void ToDomainProduct_MapsPhysicalProperties()
    {
#pragma warning disable CS0618
        var coreProduct = new MesTechStok.Core.Data.Models.Product
        {
            Name = "Heavy Item",
            SKU = "HI-001",
            Weight = 5.5m,
            Length = 30m,
            Width = 20m,
            Height = 10m,
            Desi = 3.5m
        };
#pragma warning restore CS0618

        var domain = _mapper.ToDomainProduct(coreProduct);

        Assert.Equal(5.5m, domain.Weight);
        Assert.Equal(30m, domain.Length);
        Assert.Equal(20m, domain.Width);
        Assert.Equal(10m, domain.Height);
        Assert.Equal(3.5m, domain.Desi);
    }

    [Fact]
    public void ToDomainProduct_MapsStockThresholds()
    {
#pragma warning disable CS0618
        var coreProduct = new MesTechStok.Core.Data.Models.Product
        {
            Name = "Threshold Item",
            SKU = "TH-001",
            Stock = 50,
            MinimumStock = 10,
            MaximumStock = 500,
            ReorderLevel = 20,
            ReorderQuantity = 100
        };
#pragma warning restore CS0618

        var domain = _mapper.ToDomainProduct(coreProduct);

        Assert.Equal(50, domain.Stock);
        Assert.Equal(10, domain.MinimumStock);
        Assert.Equal(500, domain.MaximumStock);
        Assert.Equal(20, domain.ReorderLevel);
        Assert.Equal(100, domain.ReorderQuantity);
    }

    [Fact]
    public void ToDomainProduct_HandlesNullStringsAsEmpty()
    {
#pragma warning disable CS0618
        var coreProduct = new MesTechStok.Core.Data.Models.Product
        {
            Name = null!,
            SKU = null!
        };
#pragma warning restore CS0618

        var domain = _mapper.ToDomainProduct(coreProduct);

        Assert.Equal(string.Empty, domain.Name);
        Assert.Equal(string.Empty, domain.SKU);
    }

    [Fact]
    public void ToDomainProduct_ThrowsOnNull()
    {
#pragma warning disable CS0618
        Assert.Throws<ArgumentNullException>(() => _mapper.ToDomainProduct(null!));
#pragma warning restore CS0618
    }

    // ── ToCoreProduct ──

    [Fact]
    public void ToCoreProduct_MapsFromDomain()
    {
        var domainProduct = new MesTech.Domain.Entities.Product
        {
            Name = "Domain Product",
            SKU = "D-SKU-001",
            SalePrice = 250m,
            Stock = 42,
            Description = "Domain description"
        };

#pragma warning disable CS0618
        var core = _mapper.ToCoreProduct(domainProduct);
#pragma warning restore CS0618

        Assert.Equal("Domain Product", core.Name);
        Assert.Equal("D-SKU-001", core.SKU);
        Assert.Equal(250m, core.SalePrice);
        Assert.Equal(42, core.Stock);
        Assert.Equal("Domain description", core.Description);
    }

    [Fact]
    public void ToCoreProduct_MapsCommerceAttributes()
    {
        var domainProduct = new MesTech.Domain.Entities.Product
        {
            Name = "Commerce Item",
            SKU = "CI-001",
            Brand = "TestBrand",
            Model = "TX-100",
            Color = "Red",
            Size = "L",
            Origin = "Turkey",
            Material = "Cotton"
        };

#pragma warning disable CS0618
        var core = _mapper.ToCoreProduct(domainProduct);
#pragma warning restore CS0618

        Assert.Equal("TestBrand", core.Brand);
        Assert.Equal("TX-100", core.Model);
        Assert.Equal("Red", core.Color);
        Assert.Equal("L", core.Size);
        Assert.Equal("Turkey", core.Origin);
        Assert.Equal("Cotton", core.Material);
    }

    [Fact]
    public void ToCoreProduct_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => _mapper.ToCoreProduct(null!));
    }

    // ── ToDomainCategory ──

    [Fact]
    public void ToDomainCategory_MapsBasicProperties()
    {
#pragma warning disable CS0618
        var coreCategory = new MesTechStok.Core.Data.Models.Category
        {
            Name = "Electronics",
            Code = "ELEC",
            Description = "Electronic items",
            IsActive = true,
            SortOrder = 5
        };
#pragma warning restore CS0618

        var domain = _mapper.ToDomainCategory(coreCategory);

        Assert.Equal("Electronics", domain.Name);
        Assert.Equal("ELEC", domain.Code);
        Assert.Equal("Electronic items", domain.Description);
        Assert.True(domain.IsActive);
        Assert.Equal(5, domain.SortOrder);
    }

    [Fact]
    public void ToDomainCategory_HandlesNullNameAsEmpty()
    {
#pragma warning disable CS0618
        var coreCategory = new MesTechStok.Core.Data.Models.Category
        {
            Name = null!
        };
#pragma warning restore CS0618

        var domain = _mapper.ToDomainCategory(coreCategory);

        Assert.Equal(string.Empty, domain.Name);
    }

    [Fact]
    public void ToDomainCategory_ThrowsOnNull()
    {
#pragma warning disable CS0618
        Assert.Throws<ArgumentNullException>(() => _mapper.ToDomainCategory(null!));
#pragma warning restore CS0618
    }

    // ── ToCoreCategory ──

    [Fact]
    public void ToCoreCategory_MapsFromDomain()
    {
        var domainCategory = new MesTech.Domain.Entities.Category
        {
            Name = "Clothing",
            Code = "CLT",
            Description = "Clothing items",
            ShowInMenu = false,
            SortOrder = 3
        };

#pragma warning disable CS0618
        var core = _mapper.ToCoreCategory(domainCategory);
#pragma warning restore CS0618

        Assert.Equal("Clothing", core.Name);
        Assert.Equal("CLT", core.Code);
        Assert.Equal("Clothing items", core.Description);
        Assert.False(core.ShowInMenu);
        Assert.Equal(3, core.SortOrder);
    }

    [Fact]
    public void ToCoreCategory_ThrowsOnNull()
    {
#pragma warning disable CS0618
        Assert.Throws<ArgumentNullException>(() => _mapper.ToCoreCategory(null!));
#pragma warning restore CS0618
    }

    // ── Round-trip ──

    [Fact]
    public void Product_RoundTrip_PreservesValues()
    {
#pragma warning disable CS0618
        var original = new MesTechStok.Core.Data.Models.Product
        {
            Name = "Round Trip Product",
            SKU = "RT-001",
            SalePrice = 199.99m,
            PurchasePrice = 99.99m,
            Stock = 100,
            MinimumStock = 5,
            Description = "Round-trip test",
            Brand = "TestBrand",
            IsActive = true,
            TaxRate = 0.18m
        };

        var domain = _mapper.ToDomainProduct(original);
        var backToCore = _mapper.ToCoreProduct(domain);
#pragma warning restore CS0618

        Assert.Equal(original.Name, backToCore.Name);
        Assert.Equal(original.SKU, backToCore.SKU);
        Assert.Equal(original.SalePrice, backToCore.SalePrice);
        Assert.Equal(original.PurchasePrice, backToCore.PurchasePrice);
        Assert.Equal(original.Stock, backToCore.Stock);
        Assert.Equal(original.MinimumStock, backToCore.MinimumStock);
        Assert.Equal(original.Description, backToCore.Description);
        Assert.Equal(original.Brand, backToCore.Brand);
        Assert.Equal(original.IsActive, backToCore.IsActive);
        Assert.Equal(original.TaxRate, backToCore.TaxRate);
    }

    [Fact]
    public void Category_RoundTrip_PreservesValues()
    {
#pragma warning disable CS0618
        var original = new MesTechStok.Core.Data.Models.Category
        {
            Name = "Round Trip Category",
            Code = "RTC",
            Description = "Category round-trip test",
            Color = "#FF0000",
            SortOrder = 7,
            IsActive = true,
            ShowInMenu = false
        };

        var domain = _mapper.ToDomainCategory(original);
        var backToCore = _mapper.ToCoreCategory(domain);
#pragma warning restore CS0618

        Assert.Equal(original.Name, backToCore.Name);
        Assert.Equal(original.Code, backToCore.Code);
        Assert.Equal(original.Description, backToCore.Description);
        Assert.Equal(original.Color, backToCore.Color);
        Assert.Equal(original.SortOrder, backToCore.SortOrder);
        Assert.Equal(original.IsActive, backToCore.IsActive);
        Assert.Equal(original.ShowInMenu, backToCore.ShowInMenu);
    }
}
