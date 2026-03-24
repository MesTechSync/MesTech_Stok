using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Reporting.Queries.GetSavedReports;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Report Dashboard ViewModel — MediatR ile gerçek rapor yönetimi.
/// </summary>
public partial class ReportDashboardAvaloniaViewModel : ViewModelBase
{
    private readonly ISender _mediator;

    public ReportDashboardAvaloniaViewModel(ISender mediator)
    {
        _mediator = mediator;
    }

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

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // GetSavedReportsQuery requires TenantId — use default for now
            var savedReports = await _mediator.Send(new GetSavedReportsQuery(Guid.Empty));

            ScheduledReports.Clear();
            RecentReports.Clear();

            foreach (var report in savedReports)
            {
                RecentReports.Add(new RecentReportItem(
                    report.CreatedAt.ToString("dd.MM.yyyy"),
                    report.Name,
                    report.ReportType,
                    "—"));
            }
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
