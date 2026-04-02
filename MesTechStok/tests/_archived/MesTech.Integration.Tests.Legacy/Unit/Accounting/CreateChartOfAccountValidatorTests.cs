using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Domain.Accounting.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateChartOfAccountValidatorTests
{
    private readonly CreateChartOfAccountValidator _validator = new();

    private static CreateChartOfAccountCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Code: "100.01",
        Name: "Kasa Hesabı",
        AccountType: AccountType.Asset,
        ParentId: null,
        Level: 2);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Code_Over20_Fails()
    {
        var cmd = ValidCommand() with { Code = "123456789012345678901" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Code_With_Letters_Fails()
    {
        var cmd = ValidCommand() with { Code = "ABC.01" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Code_Only_Digits_Passes()
    {
        var cmd = ValidCommand() with { Code = "100" };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Code_With_Dots_Passes()
    {
        var cmd = ValidCommand() with { Code = "760.01.001" };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Over200_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('N', 201) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Invalid_AccountType_Fails()
    {
        var cmd = ValidCommand() with { AccountType = (AccountType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountType");
    }

    [Fact]
    public void Level_Zero_Fails()
    {
        var cmd = ValidCommand() with { Level = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Fact]
    public void Level_Six_Fails()
    {
        var cmd = ValidCommand() with { Level = 6 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Fact]
    public void Level_One_Passes()
    {
        var cmd = ValidCommand() with { Level = 1 };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Level");
    }

    [Fact]
    public void Level_Five_Passes()
    {
        var cmd = ValidCommand() with { Level = 5 };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Level");
    }
}
