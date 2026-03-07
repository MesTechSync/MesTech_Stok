using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

public class ProductTests
{
    [Fact]
    public void AdjustStock_ShouldUpdateStockAndRaiseEvent()
    {
        var product = FakeData.CreateProduct(stock: 100);

        product.AdjustStock(50, StockMovementType.StockIn);

        product.Stock.Should().Be(150);
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents[0].Should().BeOfType<StockChangedEvent>();
    }

    [Fact]
    public void AdjustStock_Negative_ShouldDecreaseStock()
    {
        var product = FakeData.CreateProduct(stock: 100);

        product.AdjustStock(-30, StockMovementType.StockOut);

        product.Stock.Should().Be(70);
    }

    [Fact]
    public void IsLowStock_WhenStockBelowMinimum_ShouldReturnTrue()
    {
        var product = FakeData.CreateProduct(stock: 3, minimumStock: 5);

        product.IsLowStock().Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_WhenStockAboveMinimum_ShouldReturnFalse()
    {
        var product = FakeData.CreateProduct(stock: 10, minimumStock: 5);

        product.IsLowStock().Should().BeFalse();
    }

    [Fact]
    public void IsOutOfStock_WhenStockZero_ShouldReturnTrue()
    {
        var product = FakeData.CreateProduct(stock: 0);

        product.IsOutOfStock().Should().BeTrue();
    }

    [Fact]
    public void ProfitMargin_ShouldCalculateCorrectly()
    {
        var product = FakeData.CreateProduct(purchasePrice: 80, salePrice: 100);

        product.ProfitMargin.Should().Be(20m);
    }

    [Fact]
    public void ProfitMargin_WhenSalePriceZero_ShouldReturnZero()
    {
        var product = FakeData.CreateProduct(purchasePrice: 80, salePrice: 0);

        product.ProfitMargin.Should().Be(0);
    }

    [Fact]
    public void TotalValue_ShouldCalculateStockTimesPurchasePrice()
    {
        var product = FakeData.CreateProduct(stock: 10, purchasePrice: 50);

        product.TotalValue.Should().Be(500m);
    }

    [Fact]
    public void NeedsReorder_WhenStockBelowReorderLevel_ShouldReturnTrue()
    {
        var product = FakeData.CreateProduct(stock: 5);
        product.ReorderLevel = 10;

        product.NeedsReorder().Should().BeTrue();
    }

    [Fact]
    public void Product_ShouldImplementITenantEntity()
    {
        var product = FakeData.CreateProduct();
        product.TenantId = 42;

        product.TenantId.Should().Be(42);
    }

    [Fact]
    public void Product_NewFields_ShouldHaveDefaults()
    {
        var product = FakeData.CreateProduct();

        product.CurrencyCode.Should().Be("TRY");
        product.HasVariants.Should().BeFalse();
        product.BrandId.Should().BeNull();
    }
}
