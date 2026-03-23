using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class ExportPoolProductsToPlatformCommandValidatorTests
{
    private readonly ExportPoolProductsToPlatformCommandValidator _validator = new();

    private static ExportPoolProductsToPlatformCommand CreateValidCommand() => new(
        PoolId: Guid.NewGuid(),
        ProductIds: new[] { Guid.NewGuid() },
        PlatformCode: "Trendyol",
        PriceMarkupPercent: 15m,
        HideSupplierInfo: false
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PoolId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PoolId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolId");
    }

    [Fact]
    public async Task PlatformCode_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCode = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = -5m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMarkupPercent");
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenExceeds500_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = 501m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMarkupPercent");
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenExactly500_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = 500m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
