using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura raporlari ViewModel — donem secimi, KPI kartlari, platform dagilimi.
/// </summary>
public partial class InvoiceReportAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;

    // Period filter
    [ObservableProperty] private DateTimeOffset fromDate = new(new DateTime(2026, 3, 1));
    [ObservableProperty] private DateTimeOffset toDate = new(new DateTime(2026, 3, 19));
    [ObservableProperty] private string selectedPlatform = "Tumu";

    // KPI cards
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private int eFaturaCount;
    [ObservableProperty] private int eArsivCount;

    public ObservableCollection<string> PlatformList { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"
    ];

    public ObservableCollection<PlatformBreakdownDto> PlatformBreakdown { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            await Task.Delay(400);

            TotalCount = 156;
            TotalAmount = 1_248_750.50m;
            EFaturaCount = 112;
            EArsivCount = 44;

            PlatformBreakdown.Clear();
            PlatformBreakdown.Add(new() { Platform = "Trendyol", InvoiceCount = 62, TotalAmount = 524300.00m, EFaturaCount = 48, EArsivCount = 14 });
            PlatformBreakdown.Add(new() { Platform = "Hepsiburada", InvoiceCount = 38, TotalAmount = 312450.50m, EFaturaCount = 28, EArsivCount = 10 });
            PlatformBreakdown.Add(new() { Platform = "N11", InvoiceCount = 24, TotalAmount = 186200.00m, EFaturaCount = 16, EArsivCount = 8 });
            PlatformBreakdown.Add(new() { Platform = "Amazon", InvoiceCount = 18, TotalAmount = 142800.00m, EFaturaCount = 12, EArsivCount = 6 });
            PlatformBreakdown.Add(new() { Platform = "Ciceksepeti", InvoiceCount = 14, TotalAmount = 83000.00m, EFaturaCount = 8, EArsivCount = 6 });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Rapor yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ExportExcel()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(500);
            // Simulate export
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(500);
            // Simulate export
        }
        finally { IsLoading = false; }
    }
}

public class PlatformBreakdownDto
{
    public string Platform { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int EFaturaCount { get; set; }
    public int EArsivCount { get; set; }
}
