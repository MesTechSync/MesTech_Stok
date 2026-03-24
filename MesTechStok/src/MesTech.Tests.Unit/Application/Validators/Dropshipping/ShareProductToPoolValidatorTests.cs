using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class ShareProductToPoolValidatorTests
{
    private readonly ShareProductToPoolValidator _validator = new();

    private static ShareProductToPoolCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        TargetPoolId: Guid.NewGuid(),
        PoolPrice: 59.90m,
        SourceFeedId: null);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyTargetPoolId_Fails()
    {
        var cmd = ValidCommand() with { TargetPoolId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativePoolPrice_Fails()
    {
        var cmd = ValidCommand() with { PoolPrice = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
