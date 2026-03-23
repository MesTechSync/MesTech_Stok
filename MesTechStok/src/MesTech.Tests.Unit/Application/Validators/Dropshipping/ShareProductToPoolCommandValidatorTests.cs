using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class ShareProductToPoolCommandValidatorTests
{
    private readonly ShareProductToPoolCommandValidator _validator = new();

    private static ShareProductToPoolCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        TargetPoolId: Guid.NewGuid(),
        PoolPrice: 50m,
        SourceFeedId: null
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
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
    public async Task TargetPoolId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TargetPoolId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetPoolId");
    }

    [Fact]
    public async Task PoolPrice_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PoolPrice = -10m };
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
