using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cargo Tracking screen — wired to GetCargoTrackingListQuery via MediatR.
/// Displays cargo shipments with firm-based filtering, date filter, and export.
/// </summary>
public partial class CargoTrackingAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedFirm = "Tümü";

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    // HH-FIX-018: Date filter
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string selectedDateRange = "Bu Ay";
    public string[] DateRangeOptions { get; } = ["Tumu", "Bugun", "Bu Hafta", "Bu Ay", "Son 3 Ay"];

    private readonly List<CargoTrackingItemDto> _allItems = [];

    public ObservableCollection<CargoTrackingItemDto> Shipments { get; } = [];

    public ObservableCollection<string> Firms { get; } =
    [
        "Tümü",
        "Yurtiçi Kargo",
        "Aras Kargo",
        "Sürat Kargo"
    ];

    public CargoTrackingAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetCargoTrackingListQuery(_tenantProvider.GetCurrentTenantId(), 100), ct);

            _allItems.Clear();
            _allItems.AddRange(result.Select(c => new CargoTrackingItemDto
            {
                TakipNo = c.TrackingNumber ?? c.OrderNumber,
                Firma = c.CargoProvider ?? "-",
                Tarih = c.ShippedAt ?? DateTime.MinValue,
                Durum = c.Status switch
                {
                    "Delivered" => "Teslim Edildi",
                    "Shipped" => "Yolda",
                    _ => "Hazirlaniyor"
                },
                Alici = c.OrderNumber
            }));

            ApplyFilter();
        }, "Kargo verileri yuklenirken hata");
    }

    partial void OnSelectedFirmChanged(string value) => ApplyFilter();

    // HH-FIX-018: Date range quick setter
    partial void OnSelectedDateRangeChanged(string value)
    {
        var now = DateTime.Now;
        (StartDate, EndDate) = value switch
        {
            "Bugun"    => (new DateTimeOffset(now.Date), new DateTimeOffset(now)),
            "Bu Hafta" => (new DateTimeOffset(now.Date.AddDays(-(int)now.DayOfWeek + 1)), new DateTimeOffset(now)),
            "Bu Ay"    => (new DateTimeOffset(new DateTime(now.Year, now.Month, 1)), new DateTimeOffset(now)),
            "Son 3 Ay" => (new DateTimeOffset(now.AddMonths(-3)), new DateTimeOffset(now)),
            _          => ((DateTimeOffset?)null, (DateTimeOffset?)null)
        };
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Shipments.Clear();
        var filtered = (SelectedFirm == "Tümü"
            ? _allItems
            : _allItems.Where(s => s.Firma == SelectedFirm).ToList()).AsEnumerable();

        // HH-FIX-018: Date filter
        if (StartDate.HasValue)
            filtered = filtered.Where(s => s.Tarih >= StartDate.Value.DateTime);
        if (EndDate.HasValue)
            filtered = filtered.Where(s => s.Tarih <= EndDate.Value.DateTime);

        // Sort
        filtered = SortColumn switch
        {
            "TakipNo" => SortAscending ? filtered.OrderBy(x => x.TakipNo)        : filtered.OrderByDescending(x => x.TakipNo),
            "Firma"   => SortAscending ? filtered.OrderBy(x => x.Firma)          : filtered.OrderByDescending(x => x.Firma),
            "Tarih"   => SortAscending ? filtered.OrderBy(x => x.Tarih)          : filtered.OrderByDescending(x => x.Tarih),
            "Durum"   => SortAscending ? filtered.OrderBy(x => x.Durum)          : filtered.OrderByDescending(x => x.Durum),
            "Alici"   => SortAscending ? filtered.OrderBy(x => x.Alici)          : filtered.OrderByDescending(x => x.Alici),
            _         => SortAscending ? filtered.OrderByDescending(x => x.Tarih) : filtered.OrderBy(x => x.Tarih),
        };

        foreach (var item in filtered)
            Shipments.Add(item);

        TotalCount = Shipments.Count;
        IsEmpty = Shipments.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    // HH-FIX-012: Excel export
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new ExportReportCommand(Guid.Empty, "cargo", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(
                    System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Kargo verileri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class CargoTrackingItemDto
{
    public string TakipNo { get; set; } = string.Empty;
    public string Firma { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string Alici { get; set; } = string.Empty;
}
