using FluentAssertions;
using MesTech.Application.Commands.UpdateBotNotificationStatus;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Notifications;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateBotNotificationStatusValidatorTests
{
    private readonly UpdateBotNotificationStatusValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Channel_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Channel = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public async Task Channel_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Channel = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public async Task Recipient_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Recipient = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Recipient_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Recipient = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static UpdateBotNotificationStatusCommand CreateValidCommand() => new()
    {
        Channel = "SMS",
        Recipient = "+905551234567",
        Success = true,
        TenantId = Guid.NewGuid()
    };
}
