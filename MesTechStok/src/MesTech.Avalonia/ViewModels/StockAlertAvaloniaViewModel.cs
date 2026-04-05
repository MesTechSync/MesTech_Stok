using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kritik stok alarm ViewModel — 3 seviye (Tukendi/Kritik/Dusuk) renk kodlu alarm paneli.
/// I-06 Gorev 3: Aksiyon butonlari + filtre + MESA OS event entegrasyonu.
/// </summary>
public partial class StockAlertAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string currentFilter = "All";

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    private List<StockAlertItemDto> _allAlerts = [];
    public ObservableCollection<StockAlertItemDto> FilteredAlerts { get; } = [];

    public string AlertSummary
    {
        get
        {
            int outOfStock = _allAlerts.Count(a => a.Level == "OutOfStock");
            int critical = _allAlerts.Count(a => a.Level == "Critical");
            int low = _allAlerts.Count(a => a.Level == "Low");
            return $"{outOfStock} tukendi | {critical} kritik | {low} dusuk";
        }
    }

    public StockAlertAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var alerts = await _mediator.Send(new GetStockAlertsQuery(_currentUser.TenantId), ct) ?? [];

            _allAlerts = alerts.Select(a => new StockAlertItemDto
            {
                Sku = a.SKU,
                ProductName = a.Name,
                Level = a.CurrentStock <= 0
                    ? "OutOfStock"
                    : a.CurrentStock <= a.MinThreshold
                        ? "Critical"
                        : "Low",
                CurrentStock = a.CurrentStock,
                MinimumStock = a.MinThreshold,
                WarehouseName = a.Platform ?? string.Empty
            }).ToList();

            ApplyFilter();
            OnPropertyChanged(nameof(AlertSummary));
        }, "Stok uyarilari yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredAlerts.Clear();
        var filtered = CurrentFilter switch
        {
            "OutOfStock" => _allAlerts.Where(a => a.Level == "OutOfStock"),
            "Critical" => _allAlerts.Where(a => a.Level == "Critical"),
            "Low" => _allAlerts.Where(a => a.Level == "Low"),
            _ => _allAlerts.AsEnumerable()
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(a =>
                a.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.LevelText.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        filtered = SortColumn switch
        {
            "Sku"          => SortAscending ? filtered.OrderBy(x => x.Sku)           : filtered.OrderByDescending(x => x.Sku),
            "ProductName"  => SortAscending ? filtered.OrderBy(x => x.ProductName)   : filtered.OrderByDescending(x => x.ProductName),
            "Level"        => SortAscending ? filtered.OrderBy(x => x.Level)         : filtered.OrderByDescending(x => x.Level),
            "CurrentStock" => SortAscending ? filtered.OrderBy(x => x.CurrentStock)  : filtered.OrderByDescending(x => x.CurrentStock),
            "MinimumStock" => SortAscending ? filtered.OrderBy(x => x.MinimumStock)  : filtered.OrderByDescending(x => x.MinimumStock),
            _              => SortAscending ? filtered.OrderBy(x => x.ProductName)   : filtered.OrderByDescending(x => x.ProductName),
        };

        foreach (var alert in filtered)
            FilteredAlerts.Add(alert);

        IsEmpty = FilteredAlerts.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    [RelayCommand] private async Task Refresh() => await LoadAsync();
    [RelayCommand] private void FilterAll() { CurrentFilter = "All"; ApplyFilter(); }
    [RelayCommand] private void FilterOutOfStock() { CurrentFilter = "OutOfStock"; ApplyFilter(); }
    [RelayCommand] private void FilterCritical() { CurrentFilter = "Critical"; ApplyFilter(); }
    [RelayCommand] private void FilterLow() { CurrentFilter = "Low"; ApplyFilter(); }

    [RelayCommand]
    private void PlaceOrder(StockAlertItemDto? alert)
    {
        // NAV: Navigate to order create with pre-filled product SKU
    }

    [RelayCommand]
    private void ExecuteSecondaryAction(StockAlertItemDto? alert)
    {
        // NAV: Execute secondary action (transfer/adjust) based on alert level
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "stock-alerts", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Stok alarm listesi disa aktarilirken hata");
    }
}

public class StockAlertItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    // Computed
    public string LevelText => Level switch
    {
        "OutOfStock" => "TUKENDI",
        "Critical" => "KRITIK",
        "Low" => "DUSUK",
        _ => "BILINMIYOR"
    };

    public string AlertColor => Level switch
    {
        "OutOfStock" => "#DC2626",
        "Critical" => "#D97706",
        "Low" => "#EA580C",
        _ => "#64748B"
    };

    public string AlertBorderColor => Level switch
    {
        "OutOfStock" => "#FECACA",
        "Critical" => "#FDE68A",
        "Low" => "#FED7AA",
        _ => "#E0E6ED"
    };

    public string SeverityIcon => Level switch
    {
        "OutOfStock" => "!",
        "Critical" => "!",
        "Low" => "~",
        _ => "?"
    };

    public string StockInfo => Level == "OutOfStock"
        ? "Stok: 0"
        : $"Stok: {CurrentStock} (min: {MinimumStock})";

    public string SecondaryAction => Level == "OutOfStock"
        ? "Platformlarda Durdur"
        : "Transfer Et";
}
