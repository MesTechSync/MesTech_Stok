using FluentAssertions;
using MesTech.Application.Commands.MarkNotificationDelivered;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Notifications;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class MarkNotificationDeliveredValidatorTests
{
    private readonly MarkNotificationDeliveredValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
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
    public async Task TemplateName_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TemplateName = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public async Task TemplateName_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TemplateName = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public async Task Content_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Content = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public async Task Content_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Content = new string('X', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    private static MarkNotificationDeliveredCommand CreateValidCommand() => new()
    {
        TenantId = Guid.NewGuid(),
        Channel = "Email",
        Recipient = "user@example.com",
        TemplateName = "OrderConfirmation",
        Content = "Your order has been confirmed.",
        Success = true
    };
}
