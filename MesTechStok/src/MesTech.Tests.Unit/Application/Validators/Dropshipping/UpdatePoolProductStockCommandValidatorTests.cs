using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class UpdatePoolProductStockCommandValidatorTests
{
    private readonly UpdatePoolProductStockCommandValidator _validator = new();

    private static UpdatePoolProductStockCommand CreateValidCommand() => new(
        PoolProductId: Guid.NewGuid(),
        NewPrice: 75m
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

    [Fact]
    public async Task NewPrice_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { NewPrice = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPrice");
    }

    [Fact]
    public async Task NewPrice_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { NewPrice = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
