using FluentAssertions;
using MesTech.Application.Commands.UpdateCategory;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    private static UpdateCategoryCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Giyim",
        Code: "GYM",
        IsActive: true);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
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
    public void Name_Over500_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
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
    public void Code_Over500_Fails()
    {
        var cmd = ValidCommand() with { Code = new string('C', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}
