using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kritik stok alarm ViewModel — 3 seviye (Tukendi/Kritik/Dusuk) renk kodlu alarm paneli.
/// I-06 Gorev 3: Aksiyon butonlari + filtre + MESA OS event entegrasyonu.
/// </summary>
public partial class StockAlertAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string currentFilter = "All";

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

    public StockAlertAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(100); // Will be replaced with GetStockAlertsQuery via MediatR

            _allAlerts =
            [
                new() { Sku = "TS-001", ProductName = "Erkek Tisort Basic", Level = "OutOfStock", CurrentStock = 0, MinimumStock = 5, WarehouseName = "Ana Depo" },
                new() { Sku = "AY-003", ProductName = "Spor Ayakkabi Pro", Level = "OutOfStock", CurrentStock = 0, MinimumStock = 8, WarehouseName = "Ana Depo" },
                new() { Sku = "EL-007", ProductName = "Bluetooth Kulaklik", Level = "OutOfStock", CurrentStock = 0, MinimumStock = 10, WarehouseName = "Yedek Depo" },
                new() { Sku = "KZ-005", ProductName = "Kadin Kazak Kis", Level = "Critical", CurrentStock = 3, MinimumStock = 5, WarehouseName = "Ana Depo" },
                new() { Sku = "CN-008", ProductName = "Canta Laptop 15.6", Level = "Critical", CurrentStock = 2, MinimumStock = 3, WarehouseName = "Ana Depo" },
                new() { Sku = "AY-012", ProductName = "Ayakkabi Spor", Level = "Low", CurrentStock = 12, MinimumStock = 10, WarehouseName = "Depo 2" },
                new() { Sku = "GL-015", ProductName = "Gozluk Gunes UV400", Level = "Low", CurrentStock = 18, MinimumStock = 15, WarehouseName = "Ana Depo" },
                new() { Sku = "TL-020", ProductName = "Telefon Kilifi Silikon", Level = "Low", CurrentStock = 25, MinimumStock = 20, WarehouseName = "Yedek Depo" },
            ];

            ApplyFilter();
            OnPropertyChanged(nameof(AlertSummary));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Stok uyarilari yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

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

        foreach (var alert in filtered)
            FilteredAlerts.Add(alert);

        IsEmpty = FilteredAlerts.Count == 0;
    }

    [RelayCommand] private async Task Refresh() => await LoadAsync();
    [RelayCommand] private void FilterAll() { CurrentFilter = "All"; ApplyFilter(); }
    [RelayCommand] private void FilterOutOfStock() { CurrentFilter = "OutOfStock"; ApplyFilter(); }
    [RelayCommand] private void FilterCritical() { CurrentFilter = "Critical"; ApplyFilter(); }
    [RelayCommand] private void FilterLow() { CurrentFilter = "Low"; ApplyFilter(); }
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
