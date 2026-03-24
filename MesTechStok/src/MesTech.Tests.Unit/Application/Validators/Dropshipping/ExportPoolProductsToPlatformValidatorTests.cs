using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class ExportPoolProductsToPlatformValidatorTests
{
    private readonly ExportPoolProductsToPlatformValidator _validator = new();

    private static ExportPoolProductsToPlatformCommand ValidCommand() => new(
        PoolId: Guid.NewGuid(),
        ProductIds: new[] { Guid.NewGuid() },
        PlatformCode: "Trendyol",
        PriceMarkupPercent: 10m,
        HideSupplierInfo: false);

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
    public async Task EmptyPlatformCode_Fails()
    {
        var cmd = ValidCommand() with { PlatformCode = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PlatformCodeTooLong_Fails()
    {
        var cmd = ValidCommand() with { PlatformCode = new string('X', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeMarkup_Fails()
    {
        var cmd = ValidCommand() with { PriceMarkupPercent = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
