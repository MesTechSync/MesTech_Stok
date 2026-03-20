using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// NotificationSetting entity testleri.
/// ShouldNotify, quiet hours ve RequiresChannelAddress domain logic kontrolu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
[Trait("Phase", "I-11")]
public class NotificationSettingTests
{
    private static NotificationSetting CreateSetting(
        bool isEnabled = true,
        TimeOnly? quietStart = null,
        TimeOnly? quietEnd = null)
    {
        return new NotificationSetting
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Channel = NotificationChannel.Email,
            IsEnabled = isEnabled,
            NotifyOnOrderReceived = true,
            NotifyOnLowStock = true,
            NotifyOnInvoiceDue = true,
            NotifyOnPaymentReceived = true,
            NotifyOnPlatformMessage = true,
            NotifyOnAIInsight = false,
            NotifyOnBuyboxLost = true,
            NotifyOnSystemError = true,
            NotifyOnTaxDeadline = true,
            NotifyOnReportReady = true,
            QuietHoursStart = quietStart,
            QuietHoursEnd = quietEnd
        };
    }

    [Fact(DisplayName = "ShouldNotify — enabled + category active returns true")]
    public void NotificationSetting_ShouldNotify_WhenEnabledAndCategoryActive()
    {
        // Arrange
        var setting = CreateSetting(isEnabled: true);

        // Act
        var result = setting.ShouldNotify(NotificationCategory.Order);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "ShouldNotify — disabled returns false for any category")]
    public void NotificationSetting_ShouldNotNotify_WhenDisabled()
    {
        // Arrange
        var setting = CreateSetting(isEnabled: false);

        // Act & Assert
        setting.ShouldNotify(NotificationCategory.Order).Should().BeFalse();
        setting.ShouldNotify(NotificationCategory.Stock).Should().BeFalse();
        setting.ShouldNotify(NotificationCategory.System).Should().BeFalse();
    }

    [Fact(DisplayName = "ShouldNotify — during quiet hours returns false, outside returns true")]
    public void NotificationSetting_ShouldNotNotify_DuringQuietHours()
    {
        // Arrange — quiet hours 22:00 to 08:00
        var setting = CreateSetting(
            isEnabled: true,
            quietStart: new TimeOnly(22, 0),
            quietEnd: new TimeOnly(8, 0));

        // Act — 23:00 is within quiet hours (overnight range)
        var duringQuiet = setting.ShouldNotify(
            NotificationCategory.Order,
            new DateTime(2026, 3, 20, 23, 0, 0));

        // Act — 10:00 is outside quiet hours
        var outsideQuiet = setting.ShouldNotify(
            NotificationCategory.Order,
            new DateTime(2026, 3, 20, 10, 0, 0));

        // Assert
        duringQuiet.Should().BeFalse();
        outsideQuiet.Should().BeTrue();
    }

    [Fact(DisplayName = "ShouldNotify — quiet hours across midnight works correctly")]
    public void NotificationSetting_QuietHours_AcrossMidnight_ShouldWork()
    {
        // Arrange — quiet 22:00 to 06:00
        var setting = CreateSetting(
            isEnabled: true,
            quietStart: new TimeOnly(22, 0),
            quietEnd: new TimeOnly(6, 0));

        // Act — 01:00 should be quiet
        var at0100 = setting.ShouldNotify(
            NotificationCategory.Stock,
            new DateTime(2026, 3, 20, 1, 0, 0));

        // Act — 07:00 should NOT be quiet
        var at0700 = setting.ShouldNotify(
            NotificationCategory.Stock,
            new DateTime(2026, 3, 20, 7, 0, 0));

        // Assert
        at0100.Should().BeFalse("01:00 is within 22:00-06:00 quiet window");
        at0700.Should().BeTrue("07:00 is outside 22:00-06:00 quiet window");
    }

    [Fact(DisplayName = "RequiresChannelAddress — external channels require address, Push does not")]
    public void NotificationSetting_RequiresChannelAddress_ForExternalChannels()
    {
        // Arrange & Act — Email requires address
        var emailSetting = CreateSetting();
        emailSetting.Channel = NotificationChannel.Email;
        emailSetting.RequiresChannelAddress().Should().BeTrue("Email needs an address");

        // Arrange & Act — Telegram requires address
        emailSetting.Channel = NotificationChannel.Telegram;
        emailSetting.RequiresChannelAddress().Should().BeTrue("Telegram needs a chat ID");

        // Arrange & Act — SMS requires address
        emailSetting.Channel = NotificationChannel.SMS;
        emailSetting.RequiresChannelAddress().Should().BeTrue("SMS needs a phone number");

        // Arrange & Act — WhatsApp requires address
        emailSetting.Channel = NotificationChannel.WhatsApp;
        emailSetting.RequiresChannelAddress().Should().BeTrue("WhatsApp needs a phone number");

        // Arrange & Act — Push does NOT require address
        emailSetting.Channel = NotificationChannel.Push;
        emailSetting.RequiresChannelAddress().Should().BeFalse("Push uses device token, not external address");
    }
}
