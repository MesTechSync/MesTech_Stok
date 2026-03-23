using FluentAssertions;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SyncPlatformValidatorTests
{
    private readonly SyncPlatformValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformCode_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCode = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public async Task PlatformCode_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCode = new string('P', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public async Task PlatformCode_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PlatformCode = new string('P', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static SyncPlatformCommand CreateValidCommand() => new(
        PlatformCode: "TRENDYOL",
        Direction: SyncDirection.Pull
    );
}
