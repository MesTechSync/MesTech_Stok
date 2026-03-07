using FluentAssertions;
using MesTech.Domain.Services;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Barkod dogrulama servisi koruma testleri.
/// Bu testler kirilirsa = barkod validasyon mantigi bozulmus demektir.
/// </summary>
[Trait("Category", "Unit")]
public class BarcodeValidationServiceTests
{
    private readonly BarcodeValidationService _sut = new();

    [Theory]
    [InlineData("8691234567005", true)]
    [InlineData("4006381333931", true)]
    public void ValidateEAN13_ValidBarcode_ShouldPass(string barcode, bool expected)
    {
        _sut.ValidateEAN13(barcode).Should().Be(expected);
    }

    [Theory]
    [InlineData("8690000000009")]   // wrong check digit
    [InlineData("1234567890123")]   // invalid check digit
    public void ValidateEAN13_InvalidCheckDigit_ShouldFail(string barcode)
    {
        _sut.ValidateEAN13(barcode).Should().BeFalse();
    }

    [Fact]
    public void ValidateEAN13_EmptyString_ShouldFail()
    {
        _sut.ValidateEAN13("").Should().BeFalse();
    }

    [Fact]
    public void ValidateEAN13_NullString_ShouldFail()
    {
        _sut.ValidateEAN13(null!).Should().BeFalse();
    }

    [Fact]
    public void ValidateEAN13_WrongLength_ShouldFail()
    {
        _sut.ValidateEAN13("12345").Should().BeFalse();
    }

    [Fact]
    public void ValidateEAN13_NonNumeric_ShouldFail()
    {
        _sut.ValidateEAN13("ABCDEFGHIJKLM").Should().BeFalse();
    }

    [Fact]
    public void ValidateEAN8_ValidBarcode_ShouldPass()
    {
        // EAN-8: 96385074 check digit calc
        _sut.ValidateEAN8("96385074").Should().BeTrue();
    }

    [Fact]
    public void ValidateEAN8_EmptyString_ShouldFail()
    {
        _sut.ValidateEAN8("").Should().BeFalse();
    }

    [Fact]
    public void ValidateEAN8_WrongLength_ShouldFail()
    {
        _sut.ValidateEAN8("1234567").Should().BeFalse();
    }

    [Theory]
    [InlineData("8690000000008", "EAN13")]
    [InlineData("96385074", "EAN8")]
    [InlineData("012345678905", "UPC")]
    [InlineData("ABC123", "Code128")]
    public void DetectFormat_ShouldReturnCorrectType(string barcode, string expectedFormat)
    {
        _sut.DetectFormat(barcode).Should().Be(expectedFormat);
    }

    [Fact]
    public void DetectFormat_EmptyString_ShouldReturnUnknown()
    {
        _sut.DetectFormat("").Should().Be("Unknown");
    }

    [Fact]
    public void DetectFormat_Null_ShouldReturnUnknown()
    {
        _sut.DetectFormat(null!).Should().Be("Unknown");
    }
}
