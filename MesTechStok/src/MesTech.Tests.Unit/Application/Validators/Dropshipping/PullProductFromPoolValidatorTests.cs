using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class PullProductFromPoolValidatorTests
{
    private readonly PullProductFromPoolValidator _validator = new();

    private static PullProductFromPoolCommand ValidCommand() => new(
        PoolProductId: Guid.NewGuid(),
        TargetWarehouseId: Guid.NewGuid());

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPoolProductId_Fails()
    {
        var cmd = ValidCommand() with { PoolProductId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyTargetWarehouseId_Fails()
    {
        var cmd = ValidCommand() with { TargetWarehouseId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
