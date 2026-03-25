using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;

namespace MesTech.Avalonia.ViewModels;

public partial class ReportAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool hasReportResult;

    // Config
    [ObservableProperty] private ReportTypeDto? selectedReportType;
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;

    // Results
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string totalAmount = "0,00 TL";
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<ReportTypeDto> ReportTypes { get; } =
    [
        new() { Name = "Satis Raporu", Description = "Pazaryeri ve kanal bazli satis analizi" },
        new() { Name = "Stok Raporu", Description = "Depo ve urun bazli stok durumu" },
        new() { Name = "Kar/Zarar Raporu", Description = "Donemsel kar zarar analizi" },
        new() { Name = "Siparis Raporu", Description = "Siparis durumu ve teslimat istatistikleri" },
        new() { Name = "Komisyon Raporu", Description = "Pazaryeri komisyon ve kesinti detaylari" },
        new() { Name = "Kargo Raporu", Description = "Kargo firma bazli gonderim istatistikleri" },
        new() { Name = "Iade Raporu", Description = "Iade nedenleri ve tutar analizi" },
        new() { Name = "Cari Raporu", Description = "Cari hesap bakiye ve hareket ozeti" },
    ];

    public ObservableCollection<ReportItemDto> ReportItems { get; } = [];

    public ReportAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        SelectedReportType = ReportTypes[0];
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(100);
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
    private async Task GenerateReport()
    {
        if (SelectedReportType is null) return;

        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        HasReportResult = false;
        try
        {
            var from = StartDate?.UtcDateTime ?? DateTime.UtcNow.AddMonths(-1);
            var to = EndDate?.UtcDateTime ?? DateTime.UtcNow;

            var report = await _mediator.Send(new GetCashFlowReportQuery(Guid.Empty, from, to));

            ReportItems.Clear();
            foreach (var entry in report.Entries)
            {
                ReportItems.Add(new ReportItemDto
                {
                    Date = entry.EntryDate.ToString("dd.MM.yyyy"),
                    Reference = entry.Id.ToString("N")[..8].ToUpperInvariant(),
                    Description = entry.Description ?? entry.Category ?? string.Empty,
                    Category = entry.Category ?? entry.Direction,
                    AmountFormatted = $"{entry.Amount:N2} TL"
                });
            }

            TotalCount = ReportItems.Count;
            TotalAmount = $"{report.NetFlow:N2} TL";
            HasReportResult = true;
            IsEmpty = ReportItems.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Rapor olusturulamadi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(300); // Will be replaced with export logic
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Excel aktarimi basarisiz: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class ReportTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ReportItemDto
{
    public string Date { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
}
