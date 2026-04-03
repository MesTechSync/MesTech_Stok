using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for KpiCardControl — premium KPI card with trend indicator and spark line.
/// </summary>
public partial class KpiCardViewModel : ViewModelBase
{
    public KpiCardViewModel() { }

    public KpiCardViewModel(string title, string value)
    {
        _title = title;
        _value = value;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _value = "0";

    [ObservableProperty]
    private string _trendText = "+0.0%";

    [ObservableProperty]
    private string _comparisonPeriod = "vs dün";

    [ObservableProperty]
    private bool _isPositiveTrend = true;

    [ObservableProperty]
    private bool _hasSparkLine;

    [ObservableProperty]
    private IList<Point> _sparkLinePoints = new List<Point>();

    [ObservableProperty]
    private StreamGeometry? _iconData;

    [ObservableProperty]
    private IBrush _iconBackground = new SolidColorBrush(Color.Parse("#0078D4"));

    [ObservableProperty]
    private IBrush? _valueColor;

    // ── Computed trend properties ───────────────────────────────────────

    /// <summary>
    /// Trend color: green (#2E7D32) for positive, red (#D32F2F) for negative.
    /// </summary>
    public IBrush TrendColor => IsPositiveTrend
        ? new SolidColorBrush(Color.Parse("#2E7D32"))
        : new SolidColorBrush(Color.Parse("#D32F2F"));

    /// <summary>
    /// Trend arrow icon: up triangle for positive, down triangle for negative.
    /// Uses IconTrendUp / IconTrendDown StreamGeometry paths.
    /// </summary>
    public StreamGeometry TrendIcon => IsPositiveTrend
        ? StreamGeometry.Parse("M7,14L12,9L17,14H7Z")
        : StreamGeometry.Parse("M7,10L12,15L17,10H7Z");

    partial void OnIsPositiveTrendChanged(bool value)
    {
        OnPropertyChanged(nameof(TrendColor));
        OnPropertyChanged(nameof(TrendIcon));
    }
}
