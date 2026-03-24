using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class AddProductToPoolValidatorTests
{
    private readonly AddProductToPoolValidator _validator = new();

    private static AddProductToPoolCommand ValidCommand() => new(
        PoolId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        AddedFromFeedId: null,
        PoolPrice: 49.90m);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPoolId_Fails()
    {
        var cmd = ValidCommand() with { PoolId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
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

    [Fact]
    public async Task ZeroPoolPrice_Passes()
    {
        var cmd = ValidCommand() with { PoolPrice = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
