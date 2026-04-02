using FluentAssertions;
using MesTech.Domain.ValueObjects;
using Xunit;

namespace MesTech.Integration.Tests.Domain;

public class IbanTests
{
    private const string ValidGbIban = "GB29NWBK60161331926819";

    [Fact]
    public void Constructor_ValidGbIban_ShouldSucceed()
    {
        var iban = new IBAN(ValidGbIban);
        iban.Value.Should().Be(ValidGbIban);
    }

    [Fact]
    public void Constructor_NormalizesSpacesAndDashesAndCase()
    {
        var iban = new IBAN("GB29 NWBK 6016 1331 9268 19");
        iban.Value.Should().Be("GB29NWBK60161331926819");
    }

    [Fact]
    public void Constructor_WithDashes_ShouldNormalize()
    {
        var iban = new IBAN("GB29-NWBK-6016-1331-9268-19");
        iban.Value.Should().Be("GB29NWBK60161331926819");
    }

    [Fact]
    public void Constructor_LowerCase_ShouldUppercase()
    {
        var iban = new IBAN("gb29nwbk60161331926819");
        iban.Value.Should().Be("GB29NWBK60161331926819");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmpty_ShouldThrowArgumentException(string? value)
    {
        var act = () => new IBAN(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TooShort_ShouldThrowArgumentException()
    {
        var act = () => new IBAN("GB29NWBK601613");
        act.Should().Throw<ArgumentException>().WithMessage("*15-34*");
    }

    [Fact]
    public void Constructor_TooLong_ShouldThrowArgumentException()
    {
        var act = () => new IBAN("GB29NWBK6016133192681900000000000000");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidFormat_NoCountryCode_ShouldThrow()
    {
        var act = () => new IBAN("1234567890123456");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidChecksum_ShouldThrow()
    {
        var act = () => new IBAN("GB00NWBK60161331926819");
        act.Should().Throw<ArgumentException>().WithMessage("*checksum*");
    }

    [Fact]
    public void IsTurkish_GbIban_ShouldBeFalse()
    {
        var iban = new IBAN(ValidGbIban);
        iban.IsTurkish.Should().BeFalse();
    }

    [Fact]
    public void IsTurkish_ValidTrIban_ShouldBeTrue()
    {
        // TR330006100519786457841326 — verify it doesn't throw first
        var act = () => new IBAN("TR330006100519786457841326");
        act.Should().NotThrow("TR IBAN checksum should be valid");

        var iban = new IBAN("TR330006100519786457841326");
        iban.IsTurkish.Should().BeTrue();
        iban.Value.Length.Should().Be(26);
    }

    [Fact]
    public void BankCode_GbIban_ShouldBeNull()
    {
        var iban = new IBAN(ValidGbIban);
        iban.BankCode.Should().BeNull();
    }

    [Fact]
    public void BankCode_TrIban_ShouldReturn5CharBankCode()
    {
        var iban = new IBAN("TR330006100519786457841326");
        iban.BankCode.Should().Be("00061");
    }

    [Fact]
    public void Formatted_ShouldGroupInto4CharBlocks()
    {
        var iban = new IBAN(ValidGbIban);
        iban.Formatted.Should().Be("GB29 NWBK 6016 1331 9268 19");
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnValue()
    {
        var iban = new IBAN(ValidGbIban);
        string result = iban;
        result.Should().Be(ValidGbIban);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var iban = new IBAN(ValidGbIban);
        iban.ToString().Should().Be(ValidGbIban);
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var iban1 = new IBAN(ValidGbIban);
        var iban2 = new IBAN(ValidGbIban);
        iban1.Should().Be(iban2);
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        var iban1 = new IBAN(ValidGbIban);
        var iban2 = new IBAN("DE89370400440532013000");
        iban1.Should().NotBe(iban2);
    }
}

public class LocationCodeTests
{
    [Fact]
    public void FullCode_AllParts_ShouldJoinWithDashes()
    {
        var loc = new LocationCode("A", "01", "03", "05");
        loc.FullCode.Should().Be("A-01-03-05");
    }

    [Fact]
    public void FullCode_OnlyZone_ShouldReturnZone()
    {
        var loc = new LocationCode(zone: "A");
        loc.FullCode.Should().Be("A");
    }

    [Fact]
    public void FullCode_AllNull_ShouldReturnEmpty()
    {
        var loc = new LocationCode();
        loc.FullCode.Should().BeEmpty();
    }

    [Fact]
    public void IsEmpty_AllNull_ShouldBeTrue()
    {
        var loc = new LocationCode();
        loc.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData("A", null, null, null)]
    [InlineData(null, "01", null, null)]
    [InlineData(null, null, "03", null)]
    [InlineData(null, null, null, "05")]
    public void IsEmpty_AnySet_ShouldBeFalse(string? zone, string? rack, string? shelf, string? bin)
    {
        var loc = new LocationCode(zone, rack, shelf, bin);
        loc.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnFullCode()
    {
        var loc = new LocationCode("B", "02", "04", "06");
        loc.ToString().Should().Be("B-02-04-06");
    }

    [Fact]
    public void Equality_SameProperties_ShouldBeEqual()
    {
        var loc1 = new LocationCode("A", "01", "03", "05");
        var loc2 = new LocationCode("A", "01", "03", "05");
        loc1.Should().Be(loc2);
    }

    [Fact]
    public void Equality_DifferentProperties_ShouldNotBeEqual()
    {
        var loc1 = new LocationCode("A", "01", "03", "05");
        var loc2 = new LocationCode("B", "02", "04", "06");
        loc1.Should().NotBe(loc2);
    }
}

public class UnitOfMeasureTests
{
    [Fact]
    public void Piece_ShouldReturnPcsWithQuantity1()
    {
        var uom = UnitOfMeasure.Piece();
        uom.Unit.Should().Be("PCS");
        uom.Quantity.Should().Be(1);
    }

    [Fact]
    public void Kilogram_WithQuantity_ShouldReturnKg()
    {
        var uom = UnitOfMeasure.Kilogram(5);
        uom.Unit.Should().Be("KG");
        uom.Quantity.Should().Be(5);
    }

    [Theory]
    [InlineData("GR", "Gram")]
    [InlineData("LT", "Liter")]
    [InlineData("MT", "Meter")]
    [InlineData("BOX", "Box")]
    [InlineData("PLT", "Pallet")]
    public void Factory_ShouldReturnCorrectUnitCode(string expectedUnit, string factoryName)
    {
        var uom = factoryName switch
        {
            "Gram" => UnitOfMeasure.Gram(),
            "Liter" => UnitOfMeasure.Liter(),
            "Meter" => UnitOfMeasure.Meter(),
            "Box" => UnitOfMeasure.Box(),
            "Pallet" => UnitOfMeasure.Pallet(),
            _ => throw new ArgumentException($"Unknown factory: {factoryName}")
        };

        uom.Unit.Should().Be(expectedUnit);
        uom.Quantity.Should().Be(1);
    }

    [Theory]
    [InlineData(1, "PCS")]
    [InlineData(5, "KG")]
    [InlineData(2.5, "LT")]
    public void ToString_ShouldContainUnitCode(decimal qty, string unit)
    {
        var uom = new UnitOfMeasure(unit, qty);
        // Locale-agnostic: TR uses comma, EN uses dot for decimals
        uom.ToString().Should().Contain(unit);
    }

    [Fact]
    public void Equality_SameUnitAndQuantity_ShouldBeEqual()
    {
        var uom1 = UnitOfMeasure.Piece();
        var uom2 = UnitOfMeasure.Piece();
        uom1.Should().Be(uom2);
    }

    [Fact]
    public void Equality_DifferentUnit_ShouldNotBeEqual()
    {
        var uom1 = UnitOfMeasure.Piece();
        var uom2 = UnitOfMeasure.Kilogram();
        uom1.Should().NotBe(uom2);
    }

    [Fact]
    public void Equality_DifferentQuantity_ShouldNotBeEqual()
    {
        var uom1 = UnitOfMeasure.Kilogram(1);
        var uom2 = UnitOfMeasure.Kilogram(5);
        uom1.Should().NotBe(uom2);
    }
}
