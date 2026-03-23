using FluentAssertions;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class MarkUserNotificationReadValidatorTests
{
    private readonly MarkUserNotificationReadValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new MarkUserNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new MarkUserNotificationReadCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyNotificationId_ShouldFail()
    {
        var cmd = new MarkUserNotificationReadCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NotificationId");
    }
}
