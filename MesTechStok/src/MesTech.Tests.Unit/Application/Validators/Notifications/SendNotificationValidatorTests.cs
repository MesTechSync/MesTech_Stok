using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Notifications;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SendNotificationValidatorTests
{
    private readonly SendNotificationValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyChannel_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Channel = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public async Task ChannelExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Channel = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public async Task EmptyRecipient_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Recipient = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task RecipientExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Recipient = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task EmptyTemplateName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TemplateName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public async Task TemplateNameExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TemplateName = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public async Task EmptyContent_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Content = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public async Task ContentExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Content = new string('X', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    private static SendNotificationCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Channel: "email",
        Recipient: "user@mestech.com",
        TemplateName: "low_stock_alert",
        Content: "Stok seviyesi kritik"
    );
}
