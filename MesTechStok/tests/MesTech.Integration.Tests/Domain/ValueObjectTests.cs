using FluentAssertions;
using MesTech.Domain.ValueObjects;
using Xunit;

namespace MesTech.Integration.Tests.Domain;

/// <summary>
/// Value Object testleri: SKU, Money, Barcode, Address, StockLevel.
/// Her value object icin: olusturma, validation, equality, edge case.
/// </summary>
public class ValueObjectTests
{
    // ══════════════════════════════════════════════
    // SKU TESTLERI
    // ══════════════════════════════════════════════

    [Fact]
    public void SKU_Create_WithValidValue_ShouldUpperCase()
    {
        // Arrange & Act
        var sku = new SKU("sku-abc-123");

        // Assert
        sku.Value.Should().Be("SKU-ABC-123");
    }

    [Fact]
    public void SKU_Create_ShouldTrimWhitespace()
    {
        // Arrange & Act
        var sku = new SKU("  SKU-001  ");

        // Assert
        sku.Value.Should().Be("SKU-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SKU_Create_WithEmptyValue_ShouldThrow(string? value)
    {
        // Act
        var act = () => new SKU(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*SKU cannot be empty*");
    }

    [Fact]
    public void SKU_Equality_SameSKU_ShouldBeEqual()
    {
        // Arrange
        var sku1 = new SKU("ABC-123");
        var sku2 = new SKU("abc-123");

        // Assert
        sku1.Should().Be(sku2, "SKU is case-insensitive (both uppercase)");
    }

    [Fact]
    public void SKU_ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var sku = new SKU("test-sku");

        // Act
        string value = sku;

        // Assert
        value.Should().Be("TEST-SKU");
    }

    // ══════════════════════════════════════════════
    // MONEY TESTLERI
    // ══════════════════════════════════════════════

    [Fact]
    public void Money_TRY_ShouldCreateWithTRYCurrency()
    {
        // Act
        var money = Money.TRY(149.90m);

        // Assert
        money.Amount.Should().Be(149.90m);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Money_Add_SameCurrency_ShouldSum()
    {
        // Arrange
        var a = Money.TRY(100);
        var b = Money.TRY(50.50m);

        // Act
        var result = a.Add(b);

        // Assert
        result.Amount.Should().Be(150.50m);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Money_Add_DifferentCurrency_ShouldThrow()
    {
        // Arrange
        var try_ = Money.TRY(100);
        var usd = Money.USD(50);

        // Act
        var act = () => try_.Add(usd);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add TRY and USD*");
    }

    [Fact]
    public void Money_Subtract_ShouldCalculateCorrectly()
    {
        // Arrange
        var a = Money.TRY(500);
        var b = Money.TRY(123.45m);

        // Act
        var result = a.Subtract(b);

        // Assert
        result.Amount.Should().Be(376.55m);
    }

    [Fact]
    public void Money_Subtract_DifferentCurrency_ShouldThrow()
    {
        // Arrange
        var try_ = Money.TRY(100);
        var eur = Money.EUR(50);

        // Act
        var act = () => try_.Subtract(eur);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Money_Multiply_ShouldScale()
    {
        // Arrange
        var price = Money.TRY(100);

        // Act
        var total = price.Multiply(1.20m); // KDV dahil

        // Assert
        total.Amount.Should().Be(120m);
        total.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Money_Zero_ShouldCreateZeroAmount()
    {
        // Act
        var zero = Money.Zero("USD");

        // Assert
        zero.Amount.Should().Be(0);
        zero.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var a = Money.TRY(99.99m);
        var b = new Money(99.99m, "TRY");

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void Money_ToString_ShouldFormatCorrectly()
    {
        // Act
        var money = Money.TRY(1234.56m);

        // Assert
        var str = money.ToString();
        str.Should().Contain("TRY");
        (str.Contains("1,234.56") || str.Contains("1.234,56")).Should().BeTrue("locale-dependent formatting");
    }

    // ══════════════════════════════════════════════
    // BARCODE TESTLERI
    // ══════════════════════════════════════════════

    [Fact]
    public void Barcode_Create_ValidEAN13_ShouldIdentify()
    {
        // Arrange & Act
        var barcode = new Barcode("8680001000010");

        // Assert
        barcode.IsEAN13.Should().BeTrue();
        barcode.IsEAN8.Should().BeFalse();
        barcode.IsUPC.Should().BeFalse();
    }

    [Fact]
    public void Barcode_Create_ValidEAN8_ShouldIdentify()
    {
        // Arrange & Act
        var barcode = new Barcode("12345678");

        // Assert
        barcode.IsEAN8.Should().BeTrue();
        barcode.IsEAN13.Should().BeFalse();
    }

    [Fact]
    public void Barcode_Create_ValidUPC_ShouldIdentify()
    {
        // Arrange & Act
        var barcode = new Barcode("012345678905");

        // Assert
        barcode.IsUPC.Should().BeTrue();
        barcode.IsEAN13.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Barcode_Create_WithEmptyValue_ShouldThrow(string? value)
    {
        // Act
        var act = () => new Barcode(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Barcode cannot be empty*");
    }

    [Fact]
    public void Barcode_GetCountryPrefix_EAN13_ShouldReturn3Digits()
    {
        // Arrange
        var barcode = new Barcode("8680001000010");

        // Act
        var prefix = barcode.GetCountryPrefix();

        // Assert
        prefix.Should().Be("868", "Turkiye barkod prefix'i 868-869");
    }

    [Fact]
    public void Barcode_GetCountryPrefix_NonEAN13_ShouldReturnNull()
    {
        // Arrange
        var barcode = new Barcode("SHORT");

        // Act
        var prefix = barcode.GetCountryPrefix();

        // Assert
        prefix.Should().BeNull();
    }

    [Fact]
    public void Barcode_ShouldTrimWhitespace()
    {
        // Arrange & Act
        var barcode = new Barcode("  8680001000010  ");

        // Assert
        barcode.Value.Should().Be("8680001000010");
    }

    [Fact]
    public void Barcode_ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var barcode = new Barcode("ABC123");

        // Act
        string value = barcode;

        // Assert
        value.Should().Be("ABC123");
    }

    [Fact]
    public void Barcode_IsCode128_AnyLength_ShouldBeTrue()
    {
        // Arrange & Act
        var barcode = new Barcode("X");

        // Assert
        barcode.IsCode128.Should().BeTrue();
    }
}
