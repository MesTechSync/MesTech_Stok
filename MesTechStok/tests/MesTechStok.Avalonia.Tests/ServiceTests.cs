using FluentAssertions;
using MesTech.Avalonia.Services;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 12: ThemeService + FeatureGateService unit tests (G056)
// ════════════════════════════════════════════════════════

#region ThemeService

[Trait("Category", "Unit")]
[Trait("Layer", "Service")]
public class ThemeServiceTests
{
    [Fact]
    public void CurrentTheme_DefaultsToLight()
    {
        var sut = new ThemeService();
        sut.CurrentTheme.Should().Be("Light");
    }

    [Theory]
    [InlineData("Dark", "Dark")]
    [InlineData("Light", "Light")]
    [InlineData("System", "System")]
    [InlineData("invalid", "Light")]
    [InlineData("", "Light")]
    public void SetTheme_ShouldNormalizeInput(string input, string expected)
    {
        var sut = new ThemeService();
        sut.SetTheme(input);
        sut.CurrentTheme.Should().Be(expected);
    }

    [Fact]
    public void SetTheme_ShouldRaiseThemeChangedEvent()
    {
        var sut = new ThemeService();
        string? raisedTheme = null;
        sut.ThemeChanged += (_, theme) => raisedTheme = theme;

        sut.SetTheme("Dark");

        raisedTheme.Should().Be("Dark");
    }

    [Fact]
    public void SetTheme_MultipleChanges_ShouldTrackLatest()
    {
        var sut = new ThemeService();
        var themes = new List<string>();
        sut.ThemeChanged += (_, theme) => themes.Add(theme);

        sut.SetTheme("Dark");
        sut.SetTheme("System");
        sut.SetTheme("Light");

        themes.Should().Equal("Dark", "System", "Light");
        sut.CurrentTheme.Should().Be("Light");
    }

    [Fact]
    public void LoadSavedTheme_NoFile_ShouldDefaultToLight()
    {
        // Ensure no saved theme file exists for this test
        var prefsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MesTech", "theme.txt");
        if (File.Exists(prefsPath))
            File.Delete(prefsPath);

        var sut = new ThemeService();
        sut.LoadSavedTheme();
        sut.CurrentTheme.Should().Be("Light");
    }
}

#endregion

#region FeatureGateService

[Trait("Category", "Unit")]
[Trait("Layer", "Service")]
public class FeatureGateServiceTests
{
    [Fact]
    public void CurrentTier_DefaultsToUltra()
    {
        var sut = new FeatureGateService();
        sut.CurrentTier.Should().Be(SubscriptionTier.Ultra);
    }

    [Theory]
    [InlineData("Dashboard", SubscriptionTier.Light, true)]
    [InlineData("Products", SubscriptionTier.Light, true)]
    [InlineData("Orders", SubscriptionTier.Light, true)]
    [InlineData("Reports", SubscriptionTier.Light, false)]
    [InlineData("Reports", SubscriptionTier.Pro, true)]
    [InlineData("CRM", SubscriptionTier.Light, false)]
    [InlineData("CRM", SubscriptionTier.Pro, true)]
    [InlineData("AIInsight", SubscriptionTier.Pro, false)]
    [InlineData("AIInsight", SubscriptionTier.Ultra, true)]
    [InlineData("MesaBridge", SubscriptionTier.Ultra, true)]
    public void IsEnabled_ShouldRespectTierHierarchy(string feature, SubscriptionTier tier, bool expected)
    {
        var sut = new FeatureGateService();
        sut.SetTier(tier);
        sut.IsEnabled(feature).Should().Be(expected);
    }

    [Fact]
    public void IsEnabled_UnknownFeature_ShouldReturnTrue()
    {
        var sut = new FeatureGateService();
        sut.SetTier(SubscriptionTier.Light);
        sut.IsEnabled("UnknownFeature123").Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_CaseInsensitive()
    {
        var sut = new FeatureGateService();
        sut.SetTier(SubscriptionTier.Pro);
        sut.IsEnabled("reports").Should().BeTrue();
        sut.IsEnabled("REPORTS").Should().BeTrue();
        sut.IsEnabled("Reports").Should().BeTrue();
    }

    [Fact]
    public void SetTier_ShouldRaiseTierChangedEvent()
    {
        var sut = new FeatureGateService();
        SubscriptionTier? raisedTier = null;
        sut.TierChanged += (_, tier) => raisedTier = tier;

        sut.SetTier(SubscriptionTier.Pro);

        raisedTier.Should().Be(SubscriptionTier.Pro);
    }

    [Fact]
    public void LightTier_ShouldOnlyAllowBasicFeatures()
    {
        var sut = new FeatureGateService();
        sut.SetTier(SubscriptionTier.Light);

        sut.IsEnabled("Dashboard").Should().BeTrue();
        sut.IsEnabled("Products").Should().BeTrue();
        sut.IsEnabled("Orders").Should().BeTrue();
        sut.IsEnabled("Stock").Should().BeTrue();
        sut.IsEnabled("Settings").Should().BeTrue();

        sut.IsEnabled("Reports").Should().BeFalse();
        sut.IsEnabled("CRM").Should().BeFalse();
        sut.IsEnabled("Invoice").Should().BeFalse();
        sut.IsEnabled("AIInsight").Should().BeFalse();
    }

    [Fact]
    public void UltraTier_ShouldAllowEverything()
    {
        var sut = new FeatureGateService();
        sut.SetTier(SubscriptionTier.Ultra);

        sut.IsEnabled("Dashboard").Should().BeTrue();
        sut.IsEnabled("Reports").Should().BeTrue();
        sut.IsEnabled("AIInsight").Should().BeTrue();
        sut.IsEnabled("ApiAccess").Should().BeTrue();
        sut.IsEnabled("Webhook").Should().BeTrue();
    }
}

#endregion
