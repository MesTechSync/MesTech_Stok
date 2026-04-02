using FluentAssertions;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncPlatformValidatorTests
{
    private readonly SyncPlatformValidator _validator = new();

    private static SyncPlatformCommand ValidCommand() => new(
        PlatformCode: "trendyol",
        Direction: SyncDirection.Pull);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_PlatformCode_Fails()
    {
        var cmd = ValidCommand() with { PlatformCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public void PlatformCode_Over500_Fails()
    {
        var cmd = ValidCommand() with { PlatformCode = new string('P', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }
}
