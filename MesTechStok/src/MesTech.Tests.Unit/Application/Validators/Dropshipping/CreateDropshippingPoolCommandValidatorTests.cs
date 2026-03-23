using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class CreateDropshippingPoolCommandValidatorTests
{
    private readonly CreateDropshippingPoolCommandValidator _validator = new();

    private static CreateDropshippingPoolCommand CreateValidCommand() => new(
        Name: "Test Pool",
        Description: "A test pool",
        IsPublic: true,
        PricingStrategy: PoolPricingStrategy.Markup
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenTooLong_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExactly200Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 200) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PricingStrategy_WhenInvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PricingStrategy = (PoolPricingStrategy)999 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PricingStrategy");
    }

    [Theory]
    [InlineData(PoolPricingStrategy.Markup)]
    [InlineData(PoolPricingStrategy.Fixed)]
    [InlineData(PoolPricingStrategy.Dynamic)]
    public async Task PricingStrategy_WhenValidEnum_ShouldPass(PoolPricingStrategy strategy)
    {
        var cmd = CreateValidCommand() with { PricingStrategy = strategy };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
