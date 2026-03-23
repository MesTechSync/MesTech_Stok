using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class RemoveProductFromPoolCommandValidatorTests
{
    private readonly RemoveProductFromPoolCommandValidator _validator = new();

    private static RemoveProductFromPoolCommand CreateValidCommand() => new(
        PoolProductId: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PoolProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PoolProductId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolProductId");
    }
}
