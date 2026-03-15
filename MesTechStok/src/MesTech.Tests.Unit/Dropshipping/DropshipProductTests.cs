using FluentAssertions;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Exceptions;
using Xunit;

namespace MesTech.Tests.Unit.Dropshipping;

/// <summary>
/// DropshipProduct entity unit testleri — Dalga 13 Wave 1.
/// 22 tests: Create, LinkToProduct, Unlink, ApplyMarkup, UpdateStock, UpdatePrice, guards.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "DropshipProduct")]
public class DropshipProductTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();

    private static DropshipProduct CreateValidProduct(
        decimal originalPrice = 100m,
        int stockQuantity = 50)
    {
        return DropshipProduct.Create(
            TenantId,
            SupplierId,
            "EXT-PRD-001",
            "Test Dropship Urun",
            originalPrice,
            stockQuantity);
    }

    // ══════════════════════════════════════════════════════════════
    // 1. Create — valid data
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithValidData_ShouldSetAllProperties()
    {
        // Arrange & Act
        var product = DropshipProduct.Create(
            TenantId,
            SupplierId,
            "EXT-12345",
            "Premium Widget",
            250m,
            100);

        // Assert
        product.TenantId.Should().Be(TenantId);
        product.DropshipSupplierId.Should().Be(SupplierId);
        product.ExternalProductId.Should().Be("EXT-12345");
        product.Title.Should().Be("Premium Widget");
        product.OriginalPrice.Should().Be(250m);
        product.StockQuantity.Should().Be(100);
        product.IsLinked.Should().BeFalse("new product should not be linked");
        product.ProductId.Should().BeNull("new product should not have a linked product id");
    }

    // ══════════════════════════════════════════════════════════════
    // 2. Create — zero price guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithZeroPrice_ShouldThrow()
    {
        var act = () => DropshipProduct.Create(
            TenantId, SupplierId, "EXT-001", "Zero Price", 0m, 10);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 3. Create — negative price guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithNegativePrice_ShouldThrow()
    {
        var act = () => DropshipProduct.Create(
            TenantId, SupplierId, "EXT-001", "Negative Price", -10m, 10);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 4. Create — negative stock guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithNegativeStock_ShouldThrow()
    {
        var act = () => DropshipProduct.Create(
            TenantId, SupplierId, "EXT-001", "Negative Stock", 100m, -5);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 5. Create — empty title guard
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldThrow(string? title)
    {
        var act = () => DropshipProduct.Create(
            TenantId, SupplierId, "EXT-001", title!, 100m, 10);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 6. Create — empty external product id guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WithEmptyExternalProductId_ShouldThrow()
    {
        var act = () => DropshipProduct.Create(
            TenantId, SupplierId, "", "Valid Title", 100m, 10);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 7. LinkToProduct — sets ProductId and IsLinked
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void LinkToProduct_ShouldSetProductIdAndIsLinked()
    {
        var product = CreateValidProduct();
        var productId = Guid.NewGuid();

        product.LinkToProduct(productId);

        product.ProductId.Should().Be(productId);
        product.IsLinked.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════
    // 8. LinkToProduct — empty Guid guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void LinkToProduct_WithEmptyGuid_ShouldThrow()
    {
        var product = CreateValidProduct();

        var act = () => product.LinkToProduct(Guid.Empty);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 9. Unlink — clears ProductId and IsLinked
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Unlink_ShouldClearProductIdAndSetIsLinkedFalse()
    {
        var product = CreateValidProduct();
        product.LinkToProduct(Guid.NewGuid());
        product.IsLinked.Should().BeTrue();

        product.Unlink();

        product.ProductId.Should().BeNull();
        product.IsLinked.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // 10. Unlink — idempotent when already unlinked
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Unlink_WhenAlreadyUnlinked_ShouldNotThrow()
    {
        var product = CreateValidProduct();
        product.IsLinked.Should().BeFalse();

        // Act — should not throw
        product.Unlink();

        product.ProductId.Should().BeNull();
        product.IsLinked.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // 11. ApplyMarkup — Percentage (100 TL + 20% = 120 TL)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void ApplyMarkup_Percentage_ShouldCalculateSellingPriceCorrectly()
    {
        var product = CreateValidProduct(originalPrice: 100m);

        product.ApplyMarkup(DropshipMarkupType.Percentage, 20m);

        product.SellingPrice.Should().Be(120m, "100 + 20% = 120 TL");
    }

    // ══════════════════════════════════════════════════════════════
    // 12. ApplyMarkup — FixedAmount (100 TL + 15 TL = 115 TL)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void ApplyMarkup_FixedAmount_ShouldCalculateSellingPriceCorrectly()
    {
        var product = CreateValidProduct(originalPrice: 100m);

        product.ApplyMarkup(DropshipMarkupType.FixedAmount, 15m);

        product.SellingPrice.Should().Be(115m, "100 + 15 TL = 115 TL");
    }

    // ══════════════════════════════════════════════════════════════
    // 13-16. ApplyMarkup — Theory with multiple scenarios
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(200, DropshipMarkupType.Percentage, 10, 220)]     // 200 + 10% = 220
    [InlineData(200, DropshipMarkupType.Percentage, 50, 300)]     // 200 + 50% = 300
    [InlineData(200, DropshipMarkupType.FixedAmount, 25, 225)]    // 200 + 25 = 225
    [InlineData(500, DropshipMarkupType.FixedAmount, 100, 600)]   // 500 + 100 = 600
    public void ApplyMarkup_MultipleScenarios_ShouldCalculateCorrectly(
        decimal originalPrice,
        DropshipMarkupType markupType,
        decimal markupValue,
        decimal expectedSellingPrice)
    {
        var product = CreateValidProduct(originalPrice: originalPrice);

        product.ApplyMarkup(markupType, markupValue);

        product.SellingPrice.Should().Be(expectedSellingPrice);
    }

    // ══════════════════════════════════════════════════════════════
    // 17. UpdateStock — valid quantity
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateStock_WithValidQuantity_ShouldUpdateStockQuantity()
    {
        var product = CreateValidProduct(stockQuantity: 50);

        product.UpdateStock(75);

        product.StockQuantity.Should().Be(75);
    }

    // ══════════════════════════════════════════════════════════════
    // 18. UpdateStock — zero quantity is allowed
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateStock_WithZero_ShouldSetStockToZero()
    {
        var product = CreateValidProduct(stockQuantity: 50);

        product.UpdateStock(0);

        product.StockQuantity.Should().Be(0, "zero stock is valid — out of stock scenario");
    }

    // ══════════════════════════════════════════════════════════════
    // 19. UpdateStock — negative guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateStock_WithNegativeQuantity_ShouldThrow()
    {
        var product = CreateValidProduct();

        var act = () => product.UpdateStock(-1);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 20. UpdatePrice — updates OriginalPrice
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdatePrice_ShouldUpdateOriginalPrice()
    {
        var product = CreateValidProduct(originalPrice: 100m);

        product.UpdatePrice(150m);

        product.OriginalPrice.Should().Be(150m);
    }

    // ══════════════════════════════════════════════════════════════
    // 21. UpdatePrice — negative guard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UpdatePrice_WithNegativeValue_ShouldThrow()
    {
        var product = CreateValidProduct();

        var act = () => product.UpdatePrice(-1m);

        act.Should().Throw<Exception>();
    }

    // ══════════════════════════════════════════════════════════════
    // 22. SellingPrice recalculation after markup change
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void ApplyMarkup_AfterPriceUpdate_ShouldRecalculateSellingPrice()
    {
        var product = CreateValidProduct(originalPrice: 100m);

        // First markup
        product.ApplyMarkup(DropshipMarkupType.Percentage, 20m);
        product.SellingPrice.Should().Be(120m);

        // Change original price
        product.UpdatePrice(200m);

        // Re-apply markup — selling price should reflect new original
        product.ApplyMarkup(DropshipMarkupType.Percentage, 20m);
        product.SellingPrice.Should().Be(240m, "200 + 20% = 240 TL after price update");
    }
}
