using FluentAssertions;
using MesTech.Domain.Accounting.ValueObjects;

namespace MesTech.Tests.Unit.Accounting.ValueObjects;

[Trait("Category", "Unit")]
public class AccountCodeTests
{
    [Fact]
    public void Create_WithValidThreeDigitCode_ShouldSucceed()
    {
        var code = new AccountCode("100");

        code.Code.Should().Be("100");
    }

    [Theory]
    [InlineData("100")]
    [InlineData("100.01")]
    [InlineData("100.01.001")]
    [InlineData("600.05.123")]
    [InlineData("999.99.999")]
    public void Create_WithValidFormats_ShouldSucceed(string codeStr)
    {
        var code = new AccountCode(codeStr);

        code.Code.Should().Be(codeStr);
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => new AccountCode("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => new AccountCode(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespace_ShouldThrow()
    {
        var act = () => new AccountCode("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("100.AB")]
    [InlineData("10")]
    [InlineData("1000")]
    [InlineData("100.1")]
    [InlineData("100-01")]
    [InlineData("100/01")]
    [InlineData(".100")]
    [InlineData("100.")]
    public void Create_WithInvalidFormat_ShouldThrow(string invalidCode)
    {
        var act = () => new AccountCode(invalidCode);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid account code format*");
    }

    [Theory]
    [InlineData("100", 1)]
    [InlineData("100.01", 2)]
    [InlineData("100.01.001", 3)]
    public void Level_ShouldReturnCorrectLevel(string codeStr, int expectedLevel)
    {
        var code = new AccountCode(codeStr);

        code.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void Parent_ForTopLevelCode_ShouldReturnNull()
    {
        var code = new AccountCode("100");

        code.Parent.Should().BeNull();
    }

    [Fact]
    public void Parent_ForSubCode_ShouldReturnParentCode()
    {
        var code = new AccountCode("100.01");

        code.Parent.Should().NotBeNull();
        code.Parent!.Code.Should().Be("100");
    }

    [Fact]
    public void Parent_ForThirdLevelCode_ShouldReturnSecondLevel()
    {
        var code = new AccountCode("100.01.001");

        code.Parent.Should().NotBeNull();
        code.Parent!.Code.Should().Be("100.01");
    }

    [Fact]
    public void ToString_ShouldReturnCode()
    {
        var code = new AccountCode("100.01.001");

        code.ToString().Should().Be("100.01.001");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnCode()
    {
        var code = new AccountCode("100.01");
        string str = code;

        str.Should().Be("100.01");
    }

    [Fact]
    public void Equality_SameCode_ShouldBeEqual()
    {
        var code1 = new AccountCode("100.01");
        var code2 = new AccountCode("100.01");

        code1.Should().Be(code2);
        (code1 == code2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCode_ShouldNotBeEqual()
    {
        var code1 = new AccountCode("100.01");
        var code2 = new AccountCode("100.02");

        code1.Should().NotBe(code2);
    }

    [Fact]
    public void GetHashCode_SameCode_ShouldBeSame()
    {
        var code1 = new AccountCode("100.01");
        var code2 = new AccountCode("100.01");

        code1.GetHashCode().Should().Be(code2.GetHashCode());
    }
}
