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

    private void UpdateBadge(SubscriptionTier tier)
    {
        var border = this.FindControl<Border>("BadgeBorder");
        var icon = this.FindControl<TextBlock>("BadgeIcon");
        var text = this.FindControl<TextBlock>("BadgeText");
        if (border is null || icon is null || text is null) return;

        switch (tier)
        {
            case SubscriptionTier.Light:
                border.Background = new SolidColorBrush(Color.Parse("#E5E7EB"));
                icon.Text = "○";
                icon.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
                text.Text = "Light";
                text.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
                break;

            case SubscriptionTier.Pro:
                border.Background = new LinearGradientBrush
                {
                    StartPoint = new global::Avalonia.RelativePoint(0, 0, global::Avalonia.RelativeUnit.Relative),
                    EndPoint = new global::Avalonia.RelativePoint(1, 1, global::Avalonia.RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#3B82F6"), 0),
                        new GradientStop(Color.Parse("#2563EB"), 1)
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
                        new GradientStop(Color.Parse("#F59E0B"), 0),
                        new GradientStop(Color.Parse("#D97706"), 1)
                    }
                };
                icon.Text = "★";
                icon.Foreground = Brushes.White;
                text.Text = "Ultra Pro";
                text.Foreground = Brushes.White;
                break;
        }
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_featureGate is not null)
            _featureGate.TierChanged -= OnTierChanged;
        base.OnDetachedFromVisualTree(e);
    }
}
