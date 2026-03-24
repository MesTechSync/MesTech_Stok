using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

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
            await Task.Delay(500); // Will be replaced with MediatR query

            ReportItems.Clear();
            var items = new List<ReportItemDto>
            {
                new() { Date = "19.03.2026", Reference = "ORD-28341", Description = "Trendyol siparis - Elektronik", Category = "Satis", AmountFormatted = "4.520,00 TL" },
                new() { Date = "18.03.2026", Reference = "ORD-28290", Description = "Hepsiburada siparis - Ev Yasam", Category = "Satis", AmountFormatted = "2.180,00 TL" },
                new() { Date = "17.03.2026", Reference = "ORD-28245", Description = "N11 siparis - Aksesuar", Category = "Satis", AmountFormatted = "1.240,00 TL" },
                new() { Date = "16.03.2026", Reference = "ORD-28198", Description = "Amazon siparis - Kitap", Category = "Satis", AmountFormatted = "3.890,00 TL" },
                new() { Date = "15.03.2026", Reference = "ORD-28150", Description = "Trendyol siparis - Giyim", Category = "Satis", AmountFormatted = "1.650,00 TL" },
            };
            foreach (var item in items)
                ReportItems.Add(item);

            TotalCount = ReportItems.Count;
            TotalAmount = "13.480,00 TL";
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
