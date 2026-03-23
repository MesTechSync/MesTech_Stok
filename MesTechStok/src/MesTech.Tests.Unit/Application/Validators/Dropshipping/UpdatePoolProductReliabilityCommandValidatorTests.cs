using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Interfaces;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class UpdatePoolProductReliabilityCommandValidatorTests
{
    private readonly UpdatePoolProductReliabilityCommandValidator _validator = new();

    private static UpdatePoolProductReliabilityCommand CreateValidCommand() => new(
        PoolProductId: Guid.NewGuid(),
        NewScore: 85m,
        NewColor: ReliabilityColor.Yellow
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
    public async Task NewScore_WhenBelowZero_ShouldFail()
    {
        var cmd = CreateValidCommand() with { NewScore = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewScore");
    }

    [Fact]
    public async Task NewScore_WhenAbove100_ShouldFail()
    {
        var cmd = CreateValidCommand() with { NewScore = 101m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewScore");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task NewScore_WhenInRange_ShouldPass(int score)
    {
        var cmd = CreateValidCommand() with { NewScore = score };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NewColor_WhenInvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { NewColor = (ReliabilityColor)999 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewColor");
    }

    [Theory]
    [InlineData(ReliabilityColor.Red)]
    [InlineData(ReliabilityColor.Orange)]
    [InlineData(ReliabilityColor.Yellow)]
    [InlineData(ReliabilityColor.Green)]
    public async Task NewColor_WhenValidEnum_ShouldPass(ReliabilityColor color)
    {
        var cmd = CreateValidCommand() with { NewColor = color };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
