using Avalonia.Controls;
using Avalonia.Media;
using MesTech.Avalonia.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.Controls;

/// <summary>
/// G091: Plan badge — shows current subscription tier in header.
/// Light = grey, Pro = blue, Ultra = gold gradient.
/// Listens to FeatureGateService.TierChanged for live updates.
/// </summary>
public partial class PlanBadgeControl : UserControl
{
    private IFeatureGateService? _featureGate;

    public PlanBadgeControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _featureGate = App.ServiceProvider?.GetService<IFeatureGateService>();
        if (_featureGate is not null)
        {
            _featureGate.TierChanged += OnTierChanged;
            UpdateBadge(_featureGate.CurrentTier);
        }
    }

    private void OnTierChanged(object? sender, SubscriptionTier tier) => UpdateBadge(tier);

    private static Color GetTokenColor(string key) =>
        global::Avalonia.Application.Current?.Resources.TryGetResource(key, null, out var val) == true && val is Color c ? c : Colors.Gray;

    private void UpdateBadge(SubscriptionTier tier)
    {
        var border = this.FindControl<Border>("BadgeBorder");
        var icon = this.FindControl<TextBlock>("BadgeIcon");
        var text = this.FindControl<TextBlock>("BadgeText");
        if (border is null || icon is null || text is null) return;

        switch (tier)
        {
            case SubscriptionTier.Light:
                border.Background = new SolidColorBrush(GetTokenColor("MesBadgeLightBg"));
                icon.Text = "○";
                icon.Foreground = new SolidColorBrush(GetTokenColor("MesCoolGray"));
                text.Text = "Light";
                text.Foreground = new SolidColorBrush(GetTokenColor("MesCoolGray"));
                break;

            case SubscriptionTier.Pro:
                border.Background = new LinearGradientBrush
                {
                    StartPoint = new global::Avalonia.RelativePoint(0, 0, global::Avalonia.RelativeUnit.Relative),
                    EndPoint = new global::Avalonia.RelativePoint(1, 1, global::Avalonia.RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(GetTokenColor("MesBadgeProStart"), 0),
                        new GradientStop(GetTokenColor("MesBadgeProEnd"), 1)
                    }
                };
                icon.Text = "◆";
                icon.Foreground = Brushes.White;
                text.Text = "Pro";
                text.Foreground = Brushes.White;
                break;

            case SubscriptionTier.Ultra:
                border.Background = new LinearGradientBrush
                {
                    StartPoint = new global::Avalonia.RelativePoint(0, 0, global::Avalonia.RelativeUnit.Relative),
                    EndPoint = new global::Avalonia.RelativePoint(1, 1, global::Avalonia.RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(GetTokenColor("MesAmber"), 0),
                        new GradientStop(GetTokenColor("MesBadgeUltraEnd"), 1)
                    }
                };
                icon.Text = "★";
                icon.Foreground = Brushes.White;
                text.Text = "Ultra Pro";
                text.Foreground = Brushes.White;
                break;
        }
    }

    protected override void OnPointerPressed(global::Avalonia.Input.PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_featureGate is null) return;

        // Cycle through tiers: Light → Pro → Ultra → Light
        var next = _featureGate.CurrentTier switch
        {
            SubscriptionTier.Light => SubscriptionTier.Pro,
            SubscriptionTier.Pro => SubscriptionTier.Ultra,
            _ => SubscriptionTier.Light
        };
        _featureGate.SetTier(next);
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_featureGate is not null)
            _featureGate.TierChanged -= OnTierChanged;
        base.OnDetachedFromVisualTree(e);
    }
}
