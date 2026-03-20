using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Report Dashboard ViewModel — quick reports, parameters, scheduled/recent reports.
/// İ-11 Görev 4B: Central report dashboard with mock data.
/// </summary>
public partial class ReportDashboardAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isGenerating;
    [ObservableProperty] private string generatingMessage = string.Empty;
    [ObservableProperty] private int progressPercent;

    // Report parameters
    [ObservableProperty] private string selectedReportType = "Stok Degerleme";
    [ObservableProperty] private DateTimeOffset startDate = new(new DateTime(2026, 3, 1));
    [ObservableProperty] private DateTimeOffset endDate = new(new DateTime(2026, 3, 20));
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedExportFormat = "Excel";

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "Stok Degerleme", "Platform Performans", "Musteri Yasam Boyu Degeri",
        "Gonderim Raporu", "Vergi Ozeti", "Siparis Detay", "Kar-Zarar"
    };

    public ObservableCollection<string> Platforms { get; } = new()
    {
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"
    };

    public ObservableCollection<string> ExportFormats { get; } = new() { "Excel", "PDF", "CSV" };

    // Scheduled reports
    public ObservableCollection<ScheduledReportItem> ScheduledReports { get; } = new();

    // Recent reports
    public ObservableCollection<RecentReportItem> RecentReports { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate loading

            ScheduledReports.Clear();
            ScheduledReports.Add(new ScheduledReportItem("Haftalik Stok Raporu", "Haftalik", "Excel", "17.03.2026", "Aktif"));
            ScheduledReports.Add(new ScheduledReportItem("Aylik Platform Performans", "Aylik", "PDF", "01.03.2026", "Aktif"));
            ScheduledReports.Add(new ScheduledReportItem("Gunluk Siparis Ozeti", "Gunluk", "CSV", "20.03.2026", "Aktif"));
            ScheduledReports.Add(new ScheduledReportItem("Ceyreklik Vergi Raporu", "3 Aylik", "PDF", "01.01.2026", "Duraklatildi"));
            ScheduledReports.Add(new ScheduledReportItem("Haftalik CLV Analizi", "Haftalik", "Excel", "14.03.2026", "Aktif"));

            RecentReports.Clear();
            RecentReports.Add(new RecentReportItem("20.03.2026", "Stok Degerleme Raporu", "Excel", "2.4 MB"));
            RecentReports.Add(new RecentReportItem("19.03.2026", "Platform Performans", "PDF", "1.8 MB"));
            RecentReports.Add(new RecentReportItem("18.03.2026", "Gonderim Raporu", "CSV", "890 KB"));
            RecentReports.Add(new RecentReportItem("17.03.2026", "Haftalik Stok Raporu", "Excel", "3.1 MB"));
            RecentReports.Add(new RecentReportItem("15.03.2026", "CLV Analizi", "Excel", "1.2 MB"));
            RecentReports.Add(new RecentReportItem("14.03.2026", "Vergi Ozeti", "PDF", "540 KB"));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Rapor paneli yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        IsGenerating = true;
        ProgressPercent = 0;
        GeneratingMessage = $"{SelectedReportType} hazirlaniyor...";
        try
        {
            for (int i = 1; i <= 5; i++)
            {
                await Task.Delay(300);
                ProgressPercent = i * 20;
            }
            GeneratingMessage = $"{SelectedReportType} basariyla olusturuldu!";
            await Task.Delay(1000);
            GeneratingMessage = string.Empty;
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

    [RelayCommand]
    private async Task GenerateQuickReportAsync(string reportName)
    {
        IsGenerating = true;
        GeneratingMessage = $"{reportName} hazirlaniyor...";
        try
        {
            await Task.Delay(1200);
            GeneratingMessage = $"{reportName} basariyla olusturuldu!";
            await Task.Delay(800);
            GeneratingMessage = string.Empty;
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

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class ScheduledReportItem
{
    public string Name { get; }
    public string Period { get; }
    public string Format { get; }
    public string LastRun { get; }
    public string Status { get; }

    public ScheduledReportItem(string name, string period, string format, string lastRun, string status)
    {
        Name = name;
        Period = period;
        Format = format;
        LastRun = lastRun;
        Status = status;
    }
}

public class RecentReportItem
{
    public string Date { get; }
    public string ReportName { get; }
    public string Format { get; }
    public string Size { get; }

    public RecentReportItem(string date, string reportName, string format, string size)
    {
        Date = date;
        ReportName = reportName;
        Format = format;
        Size = size;
    }
}
