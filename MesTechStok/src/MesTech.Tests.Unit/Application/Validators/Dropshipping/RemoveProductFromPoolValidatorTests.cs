using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class RemoveProductFromPoolValidatorTests
{
    private readonly RemoveProductFromPoolValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new RemoveProductFromPoolCommand(PoolProductId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPoolProductId_Fails()
    {
        var cmd = new RemoveProductFromPoolCommand(PoolProductId: Guid.Empty);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
