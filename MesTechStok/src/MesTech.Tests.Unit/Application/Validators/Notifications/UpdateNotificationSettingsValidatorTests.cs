using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Notifications;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateNotificationSettingsValidatorTests
{
    private readonly UpdateNotificationSettingsValidator _sut = new();

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
    public async Task EmptyUserId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { UserId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task ChannelAddressExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ChannelAddress = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ChannelAddress");
    }

    [Fact]
    public async Task ChannelAddressNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ChannelAddress = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NegativeLowStockThreshold_ShouldFail()
    {
        var cmd = CreateValidCommand() with { LowStockThreshold = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LowStockThreshold");
    }

    [Fact]
    public async Task ZeroLowStockThreshold_ShouldPass()
    {
        var cmd = CreateValidCommand() with { LowStockThreshold = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task QuietHoursStartExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { QuietHoursStart = new string('0', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuietHoursStart");
    }

    [Fact]
    public async Task QuietHoursEndExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { QuietHoursEnd = new string('0', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuietHoursEnd");
    }

    [Fact]
    public async Task EmptyPreferredLanguage_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PreferredLanguage = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PreferredLanguage");
    }

    [Fact]
    public async Task PreferredLanguageExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PreferredLanguage = new string('L', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PreferredLanguage");
    }

    [Fact]
    public async Task DigestTimeExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DigestTime = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DigestTime");
    }

    [Fact]
    public async Task DigestTimeNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { DigestTime = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static UpdateNotificationSettingsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Channel: NotificationChannel.Email,
        ChannelAddress: "user@mestech.com",
        IsEnabled: true,
        NotifyOnOrderReceived: true,
        NotifyOnLowStock: true,
        LowStockThreshold: 5,
        NotifyOnInvoiceDue: true,
        NotifyOnPaymentReceived: true,
        NotifyOnPlatformMessage: false,
        NotifyOnAIInsight: false,
        NotifyOnBuyboxLost: true,
        NotifyOnSystemError: true,
        NotifyOnTaxDeadline: true,
        NotifyOnReportReady: false,
        QuietHoursStart: "22:00",
        QuietHoursEnd: "08:00",
        PreferredLanguage: "tr",
        DigestMode: false,
        DigestTime: null
    );
}
