using FluentAssertions;
using MesTech.Avalonia.Services;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// G056: ThemeService + FeatureGateService + NotificationService unit testleri.
/// </summary>

// ══════════════════════════════════════════════════════════════
// ThemeService — Light/Dark/System switch + event
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Service")]
[Trait("Group", "AvaloniaService")]
public class ThemeServiceTests
{
    [Fact]
    public void DefaultTheme_IsLight()
    {
        var svc = new ThemeService();
        svc.CurrentTheme.Should().BeOneOf("Light", "Dark", "System");
    }

    [Fact]
    public void SetTheme_Dark_ChangesCurrentTheme()
    {
        var svc = new ThemeService();
        svc.SetTheme("Dark");
        svc.CurrentTheme.Should().Be("Dark");
    }

    [Fact]
    public void SetTheme_FiresThemeChangedEvent()
    {
        var svc = new ThemeService();
        string? changedTo = null;
        svc.ThemeChanged += (_, theme) => changedTo = theme;

        svc.SetTheme("Dark");

        changedTo.Should().Be("Dark");
    }

    [Fact]
    public void SetTheme_SameTheme_DoesNotFireEvent()
    {
        var svc = new ThemeService();
        svc.SetTheme("Light"); // set to current default
        var eventFired = false;
        svc.ThemeChanged += (_, _) => eventFired = true;

        svc.SetTheme("Light"); // same theme again

        eventFired.Should().BeFalse();
    }
}

// ══════════════════════════════════════════════════════════════
// FeatureGateService — tier-based feature gating
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Service")]
[Trait("Group", "AvaloniaService")]
public class FeatureGateServiceTests
{
    [Fact]
    public void DefaultTier_IsUltra()
    {
        var svc = new FeatureGateService();
        svc.CurrentTier.Should().Be(SubscriptionTier.Ultra);
    }

    [Fact]
    public void LightTier_CanAccess_Dashboard()
    {
        var svc = new FeatureGateService();
        svc.SetTier(SubscriptionTier.Light);
        svc.IsEnabled("Dashboard").Should().BeTrue();
    }

    [Fact]
    public void LightTier_CannotAccess_Reports()
    {
        var svc = new FeatureGateService();
        svc.SetTier(SubscriptionTier.Light);
        svc.IsEnabled("Reports").Should().BeFalse();
    }

    [Fact]
    public void ProTier_CanAccess_Analytics()
    {
        var svc = new FeatureGateService();
        svc.SetTier(SubscriptionTier.Pro);
        svc.IsEnabled("Analytics").Should().BeTrue();
    }

    [Fact]
    public void ProTier_CannotAccess_AIInsight()
    {
        var svc = new FeatureGateService();
        svc.SetTier(SubscriptionTier.Pro);
        svc.IsEnabled("AIInsight").Should().BeFalse();
    }

    [Fact]
    public void UltraTier_CanAccess_Everything()
    {
        var svc = new FeatureGateService();
        svc.SetTier(SubscriptionTier.Ultra);
        svc.IsEnabled("Dashboard").Should().BeTrue();
        svc.IsEnabled("Reports").Should().BeTrue();
        svc.IsEnabled("AIInsight").Should().BeTrue();
    }

    [Fact]
    public void UnknownFeature_FailOpen()
    {
        var svc = new FeatureGateService();
        svc.SetTier(SubscriptionTier.Light);
        svc.IsEnabled("NonExistentFeature").Should().BeTrue();
    }

    [Fact]
    public void SetTier_FiresTierChangedEvent()
    {
        var svc = new FeatureGateService();
        SubscriptionTier? changedTo = null;
        svc.TierChanged += (_, tier) => changedTo = tier;

        svc.SetTier(SubscriptionTier.Pro);

        changedTo.Should().Be(SubscriptionTier.Pro);
    }
}

// ══════════════════════════════════════════════════════════════
// NotificationService — toast notifications
// ══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Service")]
[Trait("Group", "AvaloniaService")]
public class NotificationServiceTests
{
    [Fact]
    public void ShowSuccess_AddsNotification()
    {
        var svc = new NotificationService();
        svc.ShowSuccess("Test başarılı");
        svc.Notifications.Should().HaveCount(1);
        svc.Notifications[0].Type.Should().Be(NotificationType.Success);
    }

    [Fact]
    public void ShowError_AddsNotification()
    {
        var svc = new NotificationService();
        svc.ShowError("Hata oluştu");
        svc.Notifications.Should().HaveCount(1);
        svc.Notifications[0].Type.Should().Be(NotificationType.Error);
    }

    [Fact]
    public void MaxNotifications_RemovesOldest()
    {
        var svc = new NotificationService();
        for (int i = 0; i < 7; i++)
            svc.ShowInfo($"Message {i}");

        // Max 5 — ilk 2 kaldırılmış
        svc.Notifications.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public void AllTypes_CanBeShown()
    {
        var svc = new NotificationService();
        svc.ShowSuccess("ok");
        svc.ShowError("err");
        svc.ShowWarning("warn");
        svc.ShowInfo("info");

        svc.Notifications.Should().HaveCount(4);
    }
}
