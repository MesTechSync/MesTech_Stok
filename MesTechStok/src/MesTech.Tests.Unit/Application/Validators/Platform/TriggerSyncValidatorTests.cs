using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class TriggerSyncValidatorTests
{
    private readonly TriggerSyncValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new TriggerSyncCommand(Guid.NewGuid(), "TRENDYOL");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new TriggerSyncCommand(Guid.Empty, "TRENDYOL");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyPlatformCode_ShouldFail()
    {
        var cmd = new TriggerSyncCommand(Guid.NewGuid(), "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public async Task PlatformCodeExceeds50_ShouldFail()
    {
        var cmd = new TriggerSyncCommand(Guid.NewGuid(), new string('X', 51));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }
}
