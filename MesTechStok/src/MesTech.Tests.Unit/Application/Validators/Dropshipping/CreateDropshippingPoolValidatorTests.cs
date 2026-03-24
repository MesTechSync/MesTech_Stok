using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class CreateDropshippingPoolValidatorTests
{
    private readonly CreateDropshippingPoolValidator _validator = new();

    private static CreateDropshippingPoolCommand ValidCommand() => new(
        Name: "Test Pool",
        Description: "Test description",
        IsPublic: true,
        PricingStrategy: PoolPricingStrategy.Fixed);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyName_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NameTooLong_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('A', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DescriptionTooLong_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NullDescription_Passes()
    {
        var cmd = ValidCommand() with { Description = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
