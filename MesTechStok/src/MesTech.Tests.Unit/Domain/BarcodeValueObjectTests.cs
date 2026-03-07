using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

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
}
