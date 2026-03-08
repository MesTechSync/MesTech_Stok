using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class BarcodeValueObjectTests
{
    [Theory]
    [InlineData("8690000000008")]
    public void Create_WithValidEAN13_ShouldSucceed(string barcode)
    {
        var bc = new Barcode(barcode);

        bc.Value.Should().Be(barcode);
    }

    [Fact]
    public void Create_WithEmptyValue_ShouldThrow()
    {
        var act = () => new Barcode("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var a = new Barcode("1234567890123");
        var b = new Barcode("1234567890123");

        a.Should().Be(b);
    }

    [Fact]
    public void IsEAN13_Valid13Digit_ShouldReturnTrue()
    {
        var bc = new Barcode("8690000000008");
        bc.IsEAN13.Should().BeTrue();
    }

    [Fact]
    public void IsEAN8_Valid8Digit_ShouldReturnTrue()
    {
        var bc = new Barcode("12345678");
        bc.IsEAN8.Should().BeTrue();
    }

    [Fact]
    public void IsEAN8_NonDigit_ShouldReturnFalse()
    {
        var bc = new Barcode("1234567A");
        bc.IsEAN8.Should().BeFalse();
    }

    [Fact]
    public void IsUPC_Valid12Digit_ShouldReturnTrue()
    {
        var bc = new Barcode("012345678905");
        bc.IsUPC.Should().BeTrue();
    }

    [Fact]
    public void IsCode128_AnyNonEmpty_ShouldReturnTrue()
    {
        var bc = new Barcode("ABC-123");
        bc.IsCode128.Should().BeTrue();
    }

    [Fact]
    public void GetCountryPrefix_EAN13_ShouldReturnFirst3Digits()
    {
        var bc = new Barcode("8690000000008");
        bc.GetCountryPrefix().Should().Be("869");
    }

    [Fact]
    public void GetCountryPrefix_NonEAN13_ShouldReturnNull()
    {
        var bc = new Barcode("12345");
        bc.GetCountryPrefix().Should().BeNull();
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnValue()
    {
        var bc = new Barcode("TEST123");
        string value = bc;
        value.Should().Be("TEST123");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var bc = new Barcode("MYBARCODE");
        bc.ToString().Should().Be("MYBARCODE");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrim()
    {
        var bc = new Barcode("  8690000000008  ");
        bc.Value.Should().Be("8690000000008");
    }
}
