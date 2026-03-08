using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class LocationCodeTests
{
    [Fact]
    public void FullCode_AllParts_ShouldJoinWithDash()
    {
        var loc = new LocationCode("A", "01", "03", "05");
        loc.FullCode.Should().Be("A-01-03-05");
    }

    [Fact]
    public void FullCode_PartialParts_ShouldSkipNulls()
    {
        var loc = new LocationCode("A", rack: "02");
        loc.FullCode.Should().Be("A-02");
    }

    [Fact]
    public void IsEmpty_AllNull_ShouldReturnTrue()
    {
        var loc = new LocationCode();
        loc.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WithZone_ShouldReturnFalse()
    {
        var loc = new LocationCode("B");
        loc.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnFullCode()
    {
        var loc = new LocationCode("C", "05", "02", "10");
        loc.ToString().Should().Be("C-05-02-10");
    }

    [Fact]
    public void Properties_ShouldReturnCorrectValues()
    {
        var loc = new LocationCode("Z", "99", "01", "42");
        loc.Zone.Should().Be("Z");
        loc.Rack.Should().Be("99");
        loc.Shelf.Should().Be("01");
        loc.Bin.Should().Be("42");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a = new LocationCode("A", "01", "03", "05");
        var b = new LocationCode("A", "01", "03", "05");
        a.Should().Be(b);
    }
}
