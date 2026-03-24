using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stok Yerlesim ViewModel — depo/raf bazli urun yerlesim goruntuleme.
/// EMR-06 Gorev 4A: Sol panel depo+raf secici, sag panel raftaki urunler.
/// </summary>
public partial class StockPlacementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    [ObservableProperty] private string? selectedWarehouse;
    [ObservableProperty] private string? selectedShelf;

    public ObservableCollection<string> Warehouses { get; } = [];
    public ObservableCollection<string> Shelves { get; } = [];
    public ObservableCollection<PlacementItemDto> Items { get; } = [];

    private List<PlacementItemDto> _allItems = [];

    public StockPlacementAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(300); // Will be replaced with MediatR query

            Warehouses.Clear();
            Warehouses.Add("Ana Depo");
            Warehouses.Add("Yedek Depo");
            Warehouses.Add("Iade Depo");

            _allItems =
            [
                new() { Sku = "SKU-1001", Ad = "Samsung Galaxy S24", Miktar = 45, MinimumStock = 10, Depo = "Ana Depo", Raf = "A-01" },
                new() { Sku = "SKU-1002", Ad = "Apple MacBook Air M3", Miktar = 3, MinimumStock = 5, Depo = "Ana Depo", Raf = "A-02" },
                new() { Sku = "SKU-1003", Ad = "Sony WH-1000XM5 Kulaklik", Miktar = 78, MinimumStock = 20, Depo = "Yedek Depo", Raf = "B-01" },
                new() { Sku = "SKU-1004", Ad = "Logitech MX Master 3S", Miktar = 0, MinimumStock = 15, Depo = "Ana Depo", Raf = "A-03" },
                new() { Sku = "SKU-1005", Ad = "Dell U2723QE Monitor", Miktar = 8, MinimumStock = 5, Depo = "Yedek Depo", Raf = "B-02" },
                new() { Sku = "SKU-1006", Ad = "Anker PowerCore 20000", Miktar = 120, MinimumStock = 30, Depo = "Ana Depo", Raf = "A-01" },
                new() { Sku = "SKU-1007", Ad = "Xiaomi Mi Band 8", Miktar = 12, MinimumStock = 25, Depo = "Yedek Depo", Raf = "B-03" },
                new() { Sku = "SKU-1008", Ad = "HP LaserJet Pro M404", Miktar = 15, MinimumStock = 5, Depo = "Iade Depo", Raf = "C-01" },
                new() { Sku = "SKU-1009", Ad = "Canon EOS R50 Kamera", Miktar = 4, MinimumStock = 8, Depo = "Ana Depo", Raf = "A-02" },
                new() { Sku = "SKU-1010", Ad = "JBL Charge 5 Hoparlor", Miktar = 56, MinimumStock = 10, Depo = "Iade Depo", Raf = "C-02" },
            ];

            if (SelectedWarehouse is null && Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Yerlesim verileri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnSelectedWarehouseChanged(string? value)
    {
        UpdateShelves();
        ApplyFilters();
    }

    partial void OnSelectedShelfChanged(string? value)
    {
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            ApplyFilters();
    }

    private void UpdateShelves()
    {
        Shelves.Clear();
        if (SelectedWarehouse is null) return;

        var shelves = _allItems
            .Where(i => i.Depo == SelectedWarehouse)
            .Select(i => i.Raf)
            .Distinct()
            .OrderBy(s => s);

        foreach (var shelf in shelves)
            Shelves.Add(shelf);

        SelectedShelf = null;
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedWarehouse))
            filtered = filtered.Where(i => i.Depo == SelectedWarehouse);

        if (!string.IsNullOrWhiteSpace(SelectedShelf))
            filtered = filtered.Where(i => i.Raf == SelectedShelf);

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Ad.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class PlacementItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int Miktar { get; set; }
    public int MinimumStock { get; set; }
    public string Depo { get; set; } = string.Empty;
    public string Raf { get; set; } = string.Empty;

    /// <summary>EMR-06 renk tablosu: 4 seviye stok durumu.</summary>
    public string StokDurum
    {
        get
        {
            if (Miktar <= 0) return "TUKENDI";
            if (Miktar <= MinimumStock) return "KRITIK";
            if (Miktar <= MinimumStock * 1.5) return "DUSUK";
            return "YETERLI";
        }
    }

    public string StokDurumRenk => StokDurum switch
    {
        "TUKENDI" => "#D32F2F",
        "KRITIK" => "#D32F2F",
        "DUSUK" => "#F57C00",
        "YETERLI" => "#388E3C",
        _ => "#388E3C"
    };
}
