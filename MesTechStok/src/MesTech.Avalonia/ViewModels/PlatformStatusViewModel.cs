using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform health status for dashboard status dots.
/// Each instance represents one marketplace platform (Trendyol, HB, N11, etc.)
/// with color-coded connection/sync status.
/// Consumed by DashboardAvaloniaView.axaml DataTemplates.
/// </summary>
public partial class PlatformStatusViewModel : ViewModelBase
{
    [ObservableProperty] private string platformName = "";
    [ObservableProperty] private string tooltipText = "";
    [ObservableProperty] private IBrush statusColor = Brushes.Gray;

    /// <summary>
    /// Platform connection health status — drives the dot color on the dashboard.
    /// </summary>
    public enum PlatformHealthStatus
    {
        /// <summary>Platform connected, sync OK.</summary>
        Active,
        /// <summary>Platform connected but sync delayed or partial.</summary>
        Warning,
        /// <summary>Platform connection failed or sync error.</summary>
        Error,
        /// <summary>Platform not configured / disabled.</summary>
        Inactive
    }

    /// <summary>
    /// Update status dot color and tooltip from live platform data.
    /// </summary>
    /// <param name="status">Health status level.</param>
    /// <param name="platformName">Display name (e.g. "Trendyol").</param>
    /// <param name="lastSync">Last sync time string.</param>
    /// <param name="productCount">Active product count on this platform.</param>
    /// <param name="orderCount">Today's order count from this platform.</param>
    public void SetStatus(
        PlatformHealthStatus status,
        string platformName,
        string lastSync,
        int productCount,
        int orderCount)
    {
        PlatformName = platformName;

        StatusColor = status switch
        {
            PlatformHealthStatus.Active   => new SolidColorBrush(Color.Parse("#388E3C")),  // Green
            PlatformHealthStatus.Warning  => new SolidColorBrush(Color.Parse("#F57C00")),  // Orange
            PlatformHealthStatus.Error    => new SolidColorBrush(Color.Parse("#D32F2F")),  // Red
            PlatformHealthStatus.Inactive => new SolidColorBrush(Color.Parse("#9CA3AF")),  // Gray
            _ => Brushes.Gray
        };

        TooltipText = $"{platformName} \u2014 Son sync: {lastSync} \u2014 {productCount} \u00FCr\u00FCn, {orderCount} sipari\u015F";
    }
}
