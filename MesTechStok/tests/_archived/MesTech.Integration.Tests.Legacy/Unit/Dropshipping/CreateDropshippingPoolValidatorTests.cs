using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateDropshippingPoolValidatorTests
{
    private readonly CreateDropshippingPoolValidator _validator = new();

    private static CreateDropshippingPoolCommand ValidCommand() => new(
        Name: "Test Dropshipping Havuzu",
        Description: "Test açıklama",
        IsPublic: true,
        PricingStrategy: PoolPricingStrategy.Markup);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
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
    public void Description_Over500_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Null_Passes()
    {
        var cmd = ValidCommand() with { Description = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Name_Exactly500_Passes()
    {
        var cmd = ValidCommand() with { Name = new string('N', 500) };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Name");
    }
}
