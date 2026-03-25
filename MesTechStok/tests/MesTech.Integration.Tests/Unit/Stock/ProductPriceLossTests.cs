using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// Z10: Product.UpdatePrice zarar kontrolü.
/// SalePrice < PurchasePrice → PriceLossDetectedEvent fırlatılır.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Group", "PriceLossChain")]
public class ProductPriceLossTests
{
    private Product CreateProduct(decimal purchasePrice = 100m, decimal salePrice = 200m) =>
        new()
        {
            Name = "Test Ürün",
            SKU = "PL-001",
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            CategoryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

    [Fact]
    public void UpdatePrice_BelowPurchasePrice_RaisesPriceLossEvent()
    {
        var product = CreateProduct(purchasePrice: 100m, salePrice: 200m);
        product.UpdatePrice(80m); // 80 < 100 → zarar

        product.SalePrice.Should().Be(80m);
        product.DomainEvents.Should().ContainItemsAssignableTo<PriceLossDetectedEvent>();

        var lossEvent = product.DomainEvents.OfType<PriceLossDetectedEvent>().First();
        lossEvent.LossPerUnit.Should().Be(20m); // 100 - 80
        lossEvent.SKU.Should().Be("PL-001");
    }

    [Fact]
    public void UpdatePrice_AbovePurchasePrice_DoesNotRaiseLossEvent()
    {
        var product = CreateProduct(purchasePrice: 100m, salePrice: 200m);
        product.UpdatePrice(150m); // 150 > 100 → kâr

        product.DomainEvents.Should().NotContainItemsAssignableTo<PriceLossDetectedEvent>();
    }

    [Fact]
    public void UpdatePrice_EqualToPurchasePrice_DoesNotRaiseLossEvent()
    {
        var product = CreateProduct(purchasePrice: 100m, salePrice: 200m);
        product.UpdatePrice(100m); // 100 == 100 → zarar yok

        product.DomainEvents.Should().NotContainItemsAssignableTo<PriceLossDetectedEvent>();
    }

    [Fact]
    public void UpdatePrice_AlwaysRaisesPriceChangedEvent()
    {
        var product = CreateProduct(purchasePrice: 100m, salePrice: 200m);
        product.UpdatePrice(150m);

        product.DomainEvents.Should().ContainItemsAssignableTo<PriceChangedEvent>();
    }

    [Fact]
    public void UpdatePrice_NegativePrice_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Assert.Throws<ArgumentException>(() => product.UpdatePrice(-10m));
    }

    [Fact]
    public void UpdatePrice_SamePrice_NoEventRaised()
    {
        var product = CreateProduct(purchasePrice: 100m, salePrice: 200m);
        product.UpdatePrice(200m); // aynı fiyat

        product.DomainEvents.Should().BeEmpty();
    }
}
