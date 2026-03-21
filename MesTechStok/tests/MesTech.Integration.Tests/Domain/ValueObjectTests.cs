using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Integration.Tests.Domain;

public class SKUTests
{
    [Fact]
    public void Constructor_Should_TrimAndUppercase()
    {
        // Arrange
        var input = "  sku-001  ";

        // Act
        var sku = new SKU(input);

        // Assert
        sku.Value.Should().Be("SKU-001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrNull_Should_ThrowArgumentException(string? input)
    {
        // Arrange & Act
        var act = () => new SKU(input!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameValue_Should_BeEqual()
    {
        // Arrange
        var sku1 = new SKU("ABC-123");
        var sku2 = new SKU("abc-123");

        // Act & Assert
        sku1.Should().Be(sku2);
    }

    [Fact]
    public void Equality_DifferentValue_Should_NotBeEqual()
    {
        // Arrange
        var sku1 = new SKU("ABC-123");
        var sku2 = new SKU("XYZ-999");

        // Act & Assert
        sku1.Should().NotBe(sku2);
    }

    [Fact]
    public void ToString_Should_ReturnValue()
    {
        // Arrange
        var sku = new SKU("test-sku");

        // Act
        var result = sku.ToString();

        // Assert
        result.Should().Be("TEST-SKU");
    }

    [Fact]
    public void ImplicitStringConversion_Should_Work()
    {
        // Arrange
        var sku = new SKU("prod-001");

        // Act
        string value = sku;

        // Assert
        value.Should().Be("PROD-001");
    }
}

public class BarcodeTests
{
    [Fact]
    public void Constructor_Should_Trim()
    {
        // Arrange
        var input = "  123  ";

        // Act
        var barcode = new Barcode(input);

        // Assert
        barcode.Value.Should().Be("123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrNull_Should_ThrowArgumentException(string? input)
    {
        // Arrange & Act
        var act = () => new Barcode(input!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsEAN13_With13DigitString_Should_ReturnTrue()
    {
        // Arrange
        var barcode = new Barcode("8680001000010");

        // Act & Assert
        barcode.IsEAN13.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("123")]
    public void IsEAN13_WithNon13DigitString_Should_ReturnFalse(string input)
    {
        // Arrange
        var barcode = new Barcode(input);

        // Act & Assert
        barcode.IsEAN13.Should().BeFalse();
    }

    [Fact]
    public void IsEAN8_With8DigitString_Should_ReturnTrue()
    {
        // Arrange
        var barcode = new Barcode("12345678");

        // Act & Assert
        barcode.IsEAN8.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("ABCDEFGH")]
    public void IsEAN8_WithNon8DigitString_Should_ReturnFalse(string input)
    {
        // Arrange
        var barcode = new Barcode(input);

        // Act & Assert
        barcode.IsEAN8.Should().BeFalse();
    }

    [Fact]
    public void IsUPC_With12DigitString_Should_ReturnTrue()
    {
        // Arrange
        var barcode = new Barcode("012345678901");

        // Act & Assert
        barcode.IsUPC.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567890123")]
    [InlineData("ABCDEFGHIJKL")]
    public void IsUPC_WithNon12DigitString_Should_ReturnFalse(string input)
    {
        // Arrange
        var barcode = new Barcode(input);

        // Act & Assert
        barcode.IsUPC.Should().BeFalse();
    }

    [Fact]
    public void IsCode128_AnyNonEmpty_Should_ReturnTrue()
    {
        // Arrange
        var barcode = new Barcode("ANY-VALUE");

        // Act & Assert
        barcode.IsCode128.Should().BeTrue();
    }

    [Fact]
    public void GetCountryPrefix_EAN13_Should_ReturnFirst3Digits()
    {
        // Arrange
        var barcode = new Barcode("8680001000010");

        // Act
        var prefix = barcode.GetCountryPrefix();

        // Assert
        prefix.Should().Be("868");
    }

    [Fact]
    public void GetCountryPrefix_NonEAN13_Should_ReturnNull()
    {
        // Arrange
        var barcode = new Barcode("12345678");

        // Act
        var prefix = barcode.GetCountryPrefix();

        // Assert
        prefix.Should().BeNull();
    }

    [Fact]
    public void Equality_SameValue_Should_BeEqual()
    {
        // Arrange
        var barcode1 = new Barcode("8680001000010");
        var barcode2 = new Barcode("8680001000010");

        // Act & Assert
        barcode1.Should().Be(barcode2);
    }

    [Fact]
    public void Equality_DifferentValue_Should_NotBeEqual()
    {
        // Arrange
        var barcode1 = new Barcode("8680001000010");
        var barcode2 = new Barcode("1234567890123");

        // Act & Assert
        barcode1.Should().NotBe(barcode2);
    }

    [Fact]
    public void ImplicitStringConversion_Should_Work()
    {
        // Arrange
        var barcode = new Barcode("8680001000010");

        // Act
        string value = barcode;

        // Assert
        value.Should().Be("8680001000010");
    }
}

public class AddressTests
{
    [Fact]
    public void DefaultCountry_Should_BeTR()
    {
        // Arrange & Act
        var address = new Address
        {
            Street = "Test Sokak",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34000"
        };

        // Assert
        address.Country.Should().Be("TR");
    }

    [Fact]
    public void FullAddress_Should_ConcatenateCorrectly()
    {
        // Arrange
        var address = new Address
        {
            Street = "Ataturk Cad. No:1",
            District = "Besiktas",
            City = "Istanbul",
            PostalCode = "34353",
            Country = "TR"
        };

        // Act
        var fullAddress = address.FullAddress;

        // Assert
        fullAddress.Should().Be("Ataturk Cad. No:1, Besiktas, Istanbul 34353, TR");
    }

    [Fact]
    public void Equality_SameProperties_Should_BeEqual()
    {
        // Arrange
        var address1 = new Address
        {
            Street = "Test Sokak",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34000",
            Country = "TR"
        };
        var address2 = new Address
        {
            Street = "Test Sokak",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34000",
            Country = "TR"
        };

        // Act & Assert
        address1.Should().Be(address2);
    }

    [Fact]
    public void Equality_DifferentProperties_Should_NotBeEqual()
    {
        // Arrange
        var address1 = new Address
        {
            Street = "Test Sokak",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34000"
        };
        var address2 = new Address
        {
            Street = "Baska Sokak",
            District = "Uskudar",
            City = "Istanbul",
            PostalCode = "34700"
        };

        // Act & Assert
        address1.Should().NotBe(address2);
    }
}
