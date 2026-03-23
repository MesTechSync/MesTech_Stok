using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class AddProductToPoolCommandValidatorTests
{
    private readonly AddProductToPoolCommandValidator _validator = new();

    private static AddProductToPoolCommand CreateValidCommand() => new(
        PoolId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        AddedFromFeedId: null,
        PoolPrice: 100m
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PoolId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PoolId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolId");
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task PoolPrice_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PoolPrice = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolPrice");
    }

    [Fact]
    public async Task PoolPrice_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PoolPrice = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
