using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class UpdatePoolProductStockValidatorTests
{
    private readonly UpdatePoolProductStockValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new UpdatePoolProductStockCommand(
            PoolProductId: Guid.NewGuid(),
            NewPrice: 49.90m);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPoolProductId_Fails()
    {
        var cmd = new UpdatePoolProductStockCommand(
            PoolProductId: Guid.Empty,
            NewPrice: 49.90m);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativePrice_Fails()
    {
        var cmd = new UpdatePoolProductStockCommand(
            PoolProductId: Guid.NewGuid(),
            NewPrice: -1m);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroPrice_Passes()
    {
        var cmd = new UpdatePoolProductStockCommand(
            PoolProductId: Guid.NewGuid(),
            NewPrice: 0m);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
