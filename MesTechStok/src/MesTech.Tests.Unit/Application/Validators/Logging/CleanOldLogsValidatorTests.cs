using FluentAssertions;
using MesTech.Application.Features.Logging.Commands.CleanOldLogs;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Logging;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CleanOldLogsValidatorTests
{
    private readonly CleanOldLogsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 30);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = new CleanOldLogsCommand(Guid.Empty, 30);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task DaysToKeep_WhenZero_ShouldFail()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 0);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DaysToKeep");
    }

    [Fact]
    public async Task DaysToKeep_WhenNegative_ShouldFail()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), -5);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DaysToKeep");
    }

    [Fact]
    public async Task DaysToKeep_WhenExceeds365_ShouldFail()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 366);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DaysToKeep");
    }

    [Fact]
    public async Task DaysToKeep_WhenExactly365_ShouldPass()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 365);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DaysToKeep_WhenExactly1_ShouldPass()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 1);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
