using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura raporlari ViewModel — donem secimi, KPI kartlari, platform dagilimi.
/// </summary>
public partial class InvoiceReportAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    private readonly ICurrentUserService _currentUser;

    public InvoiceReportAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    // Period filter
    [ObservableProperty] private string _statusMessage = string.Empty;
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

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            PlatformType? platformFilter = SelectedPlatform switch
            {
                "Trendyol" => PlatformType.Trendyol,
                "Hepsiburada" => PlatformType.Hepsiburada,
                "N11" => PlatformType.N11,
                "Amazon" => PlatformType.Amazon,
                "Ciceksepeti" => PlatformType.Ciceksepeti,
                _ => null
            };

            var report = await _mediator.Send(
                new GetInvoiceReportQuery(
                    FromDate.DateTime,
                    ToDate.DateTime,
                    platformFilter),
                CancellationToken);

            TotalCount = report.TotalCount;
            TotalAmount = report.TotalAmount;
            EFaturaCount = report.EFaturaCount;
            EArsivCount = report.EArsivCount;

            PlatformBreakdown.Clear();
            foreach (var bp in report.ByPlatform)
            {
                PlatformBreakdown.Add(new()
                {
                    Platform = bp.PlatformName,
                    InvoiceCount = bp.Count,
                    TotalAmount = bp.Amount,
                    // ByPlatform DTO does not split e-Fatura/e-Arsiv per platform
                    EFaturaCount = 0,
                    EArsivCount = 0
                });
            }

            IsEmpty = TotalCount == 0;
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
            var result = await _mediator.Send(new ExportInvoiceReportCommand(
                _currentUser.TenantId, "xlsx"));
            await SaveExportFile(result.FileData, result.FileName);
            StatusMessage = $"Excel raporu kaydedildi ({result.ExportedCount} fatura).";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new ExportInvoiceReportCommand(
                _currentUser.TenantId, "pdf"));
            await SaveExportFile(result.FileData, result.FileName);
            StatusMessage = $"PDF raporu kaydedildi ({result.ExportedCount} fatura).";
        }
        finally { IsLoading = false; }
    }

    private static async Task SaveExportFile(byte[] data, string fileName)
    {
        if (data.Length == 0) return;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(Path.Combine(dir, fileName), data);
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
