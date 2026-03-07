using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class SKUValueObjectTests
{
    [Fact]
    public void Create_ShouldTrimAndUppercase()
    {
        var sku = new SKU("  abc-123  ");

        sku.Value.Should().Be("ABC-123");
    }

    [Fact]
    public void Create_WithEmptyValue_ShouldThrow()
    {
        var act = () => new SKU("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var a = new SKU("SKU-001");
        var b = new SKU("sku-001");

        a.Should().Be(b);
    }
}
