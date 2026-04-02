using Avalonia;
using Avalonia.Controls;

namespace MesTech.Avalonia.Controls;

/// <summary>
/// SkeletonLoader — Shimmer/pulse placeholder control for loading state.
/// Shows realistic content placeholders instead of generic spinners.
/// [ENT-DEV2]
/// </summary>
public partial class SkeletonLoader : UserControl
{
    public static readonly StyledProperty<bool> ShowKpiProperty =
        AvaloniaProperty.Register<SkeletonLoader, bool>(nameof(ShowKpi), defaultValue: true);

    public static readonly StyledProperty<bool> ShowChartProperty =
        AvaloniaProperty.Register<SkeletonLoader, bool>(nameof(ShowChart), defaultValue: false);

    public bool ShowKpi
    {
        get => GetValue(ShowKpiProperty);
        set => SetValue(ShowKpiProperty, value);
    }

    public bool ShowChart
    {
        get => GetValue(ShowChartProperty);
        set => SetValue(ShowChartProperty, value);
    }

    public SkeletonLoader()
    {
        InitializeComponent();
    }
}
