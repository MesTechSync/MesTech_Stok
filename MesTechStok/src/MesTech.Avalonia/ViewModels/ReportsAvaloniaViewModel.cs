using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Reports screen — Dalga 14/15.
/// Provides date range selection and report generation with loading state.
/// </summary>
public partial class ReportsAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string summary = "Rapor olusturmak icin tarih araligi secin ve rapor turunu belirleyin.";
    [ObservableProperty] private DateTimeOffset startDate = new(new DateTime(2026, 3, 1));
    [ObservableProperty] private DateTimeOffset endDate = new(new DateTime(2026, 3, 17));
    [ObservableProperty] private bool isGenerating;
    [ObservableProperty] private string generatingMessage = string.Empty;

    // Sales Report
    [ObservableProperty] private string totalSales = "0 TL";
    [ObservableProperty] private string totalOrders = "0";
    [ObservableProperty] private string averageOrderValue = "0 TL";

    // Stock Report
    [ObservableProperty] private string totalProducts = "0";
    [ObservableProperty] private string lowStockCount = "0";
    [ObservableProperty] private string outOfStockCount = "0";

    // Revenue Report
    [ObservableProperty] private string totalRevenue = "0 TL";
    [ObservableProperty] private string totalExpenses = "0 TL";
    [ObservableProperty] private string netProfit = "0 TL";

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load
            Summary = "Rapor olusturmak icin tarih araligi secin ve rapor turunu belirleyin.";

            // Load summary card data
            TotalSales = "1.245.670 TL";
            TotalOrders = "3.456";
            AverageOrderValue = "360,50 TL";

            TotalProducts = "4.812";
            LowStockCount = "127";
            OutOfStockCount = "34";

            TotalRevenue = "1.245.670 TL";
            TotalExpenses = "876.340 TL";
            NetProfit = "369.330 TL";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Raporlar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task GenerateReport(string reportType)
    {
        IsGenerating = true;
        GeneratingMessage = $"{reportType} hazirlaniyor...";
        try
        {
            await Task.Delay(1500); // Simulate report generation
            GeneratingMessage = string.Empty;
            Summary = $"{reportType} basariyla olusturuldu. ({StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy})";
        }
        catch (Exception ex)
        {
            GeneratingMessage = string.Empty;
            HasError = true;
            ErrorMessage = $"Rapor olusturulamadi: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }
}
