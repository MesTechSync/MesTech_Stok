using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stock alert item with 4-level color coding for the dashboard critical stock list.
/// Each instance represents one product with low/critical/out-of-stock status.
/// Consumed by DashboardAvaloniaView.axaml DataTemplates.
/// </summary>
public partial class StockAlertItemViewModel : ViewModelBase
{
    [ObservableProperty] private string productName = "";
    [ObservableProperty] private string sku = "";
    [ObservableProperty] private string stockText = "0";
    [ObservableProperty] private IBrush stockLevelColor = Brushes.Red;

    /// <summary>
    /// Set stock level display and apply 4-level color coding.
    /// Color thresholds (from theme tokens):
    ///   Normal  (green  #388E3C): stock > minimumStock * 2
    ///   Low     (orange #F57C00): minimumStock &lt; stock &lt;= minimumStock * 2
    ///   Critical(red    #D32F2F): 0 &lt; stock &lt;= minimumStock
    ///   Out     (dark   #1F2937): stock = 0
    /// </summary>
    /// <param name="stock">Current stock quantity.</param>
    /// <param name="minimumStock">Minimum stock threshold for this product.</param>
    public void SetStockLevel(int stock, int minimumStock)
    {
        StockText = stock.ToString();

        StockLevelColor = stock switch
        {
            0 => new SolidColorBrush(Color.Parse("#1F2937")),                                // T\u00FCkendi (dark)
            _ when stock <= minimumStock => new SolidColorBrush(Color.Parse("#D32F2F")),      // Kritik (red)
            _ when stock <= minimumStock * 2 => new SolidColorBrush(Color.Parse("#F57C00")),  // D\u00FC\u015F\u00FCk (orange)
            _ => new SolidColorBrush(Color.Parse("#388E3C"))                                  // Yeterli (green)
        };
    }

    /// <summary>
    /// Quick order command — will navigate to rapid reorder form for this product.
    /// Connected to navigation service in a later sprint.
    /// </summary>
    [RelayCommand]
    private void QuickOrder()
    {
        // Will be connected to navigation service later
    }
}
