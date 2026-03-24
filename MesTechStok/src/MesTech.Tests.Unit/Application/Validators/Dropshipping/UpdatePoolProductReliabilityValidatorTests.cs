using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Interfaces;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class UpdatePoolProductReliabilityValidatorTests
{
    private readonly UpdatePoolProductReliabilityValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new UpdatePoolProductReliabilityCommand(
            PoolProductId: Guid.NewGuid(),
            NewScore: 85m,
            NewColor: ReliabilityColor.Green);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPoolProductId_Fails()
    {
        var cmd = new UpdatePoolProductReliabilityCommand(
            PoolProductId: Guid.Empty,
            NewScore: 85m,
            NewColor: ReliabilityColor.Green);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
