using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Envanter yonetimi ViewModel — 4 seviye stok badge + depo filtre + KPI kartlari + sayfalama.
/// EMR-06 Gorev 4E: Enhanced from basic DataGrid to full-featured inventory view.
/// Will be wired to GetInventorySummaryQuery via MediatR when full migration starts.
/// </summary>
public partial class InventoryAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int alarmCount;

    // KPI
    [ObservableProperty] private int kpiTotal;
    [ObservableProperty] private int kpiCritical;
    [ObservableProperty] private decimal kpiStockValue;
    [ObservableProperty] private int kpiOutOfStock;

    // Warehouse filter
    [ObservableProperty] private string? selectedWarehouse;
    public ObservableCollection<string> WarehouseFilter { get; } = [];

    // Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private bool canGoPrevious;
    [ObservableProperty] private bool canGoNext;
    [ObservableProperty] private string paginationInfo = string.Empty;
    private const int PageSize = 25;

    public ObservableCollection<InventoryItemDto> Items { get; } = [];
    private List<InventoryItemDto> _allItems = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300);

            _allItems =
            [
                new() { Sku = "SKU-1001", Ad = "Samsung Galaxy S24", Miktar = 45, MinStok = 10, MaxStok = 200, Depo = "Ana Depo", UnitPrice = 54999.99m },
                new() { Sku = "SKU-1002", Ad = "Apple MacBook Air M3", Miktar = 3, MinStok = 5, MaxStok = 50, Depo = "Ana Depo", UnitPrice = 42999.00m },
                new() { Sku = "SKU-1003", Ad = "Sony WH-1000XM5 Kulaklik", Miktar = 78, MinStok = 20, MaxStok = 300, Depo = "Yedek Depo", UnitPrice = 8499.00m },
                new() { Sku = "SKU-1004", Ad = "Logitech MX Master 3S", Miktar = 2, MinStok = 15, MaxStok = 150, Depo = "Ana Depo", UnitPrice = 3299.00m },
                new() { Sku = "SKU-1005", Ad = "Dell U2723QE Monitor", Miktar = 8, MinStok = 5, MaxStok = 40, Depo = "Yedek Depo", UnitPrice = 12499.00m },
                new() { Sku = "SKU-1006", Ad = "Anker PowerCore 20000", Miktar = 120, MinStok = 30, MaxStok = 500, Depo = "Ana Depo", UnitPrice = 449.90m },
                new() { Sku = "SKU-1007", Ad = "Xiaomi Mi Band 8", Miktar = 0, MinStok = 25, MaxStok = 400, Depo = "Yedek Depo", UnitPrice = 999.00m },
                new() { Sku = "SKU-1008", Ad = "HP LaserJet Pro M404", Miktar = 15, MinStok = 5, MaxStok = 30, Depo = "Ana Depo", UnitPrice = 7899.00m },
                new() { Sku = "SKU-1009", Ad = "Canon EOS R50 Kamera", Miktar = 4, MinStok = 8, MaxStok = 25, Depo = "Yedek Depo", UnitPrice = 24999.00m },
                new() { Sku = "SKU-1010", Ad = "JBL Charge 5 Hoparlor", Miktar = 56, MinStok = 10, MaxStok = 200, Depo = "Ana Depo", UnitPrice = 3199.00m },
            ];

            // Populate warehouse filter
            WarehouseFilter.Clear();
            WarehouseFilter.Add("Tum Depolar");
            foreach (var w in _allItems.Select(i => i.Depo).Distinct().OrderBy(w => w))
                WarehouseFilter.Add(w);

            if (SelectedWarehouse is null)
                SelectedWarehouse = "Tum Depolar";

            // KPI
            KpiTotal = _allItems.Count;
            KpiCritical = _allItems.Count(i => i.Miktar > 0 && i.Miktar <= i.MinStok);
            KpiStockValue = _allItems.Sum(i => i.Miktar * i.UnitPrice);
            KpiOutOfStock = _allItems.Count(i => i.Miktar <= 0);

            CurrentPage = 1;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Envanter yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedWarehouse) && SelectedWarehouse != "Tum Depolar")
            filtered = filtered.Where(i => i.Depo == SelectedWarehouse);

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Ad.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Depo.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();
        TotalCount = filteredList.Count;
        AlarmCount = filteredList.Count(i => i.Miktar < i.MinStok);

        // Pagination
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)filteredList.Count / PageSize));
        if (CurrentPage > totalPages) CurrentPage = totalPages;

        var paged = filteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Items.Clear();
        foreach (var item in paged)
            Items.Add(item);

        IsEmpty = filteredList.Count == 0;
        CanGoPrevious = CurrentPage > 1;
        CanGoNext = CurrentPage < totalPages;
        PaginationInfo = $"{filteredList.Count} urunden {(CurrentPage - 1) * PageSize + 1}-{Math.Min(CurrentPage * PageSize, filteredList.Count)} gosteriliyor";
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void NextPage()
    {
        CurrentPage++;
        ApplyFilters();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        CurrentPage--;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
        {
            CurrentPage = 1;
            ApplyFilters();
        }
    }

    partial void OnSelectedWarehouseChanged(string? value)
    {
        if (_allItems.Count > 0)
        {
            CurrentPage = 1;
            ApplyFilters();
        }
    }
}

public class InventoryItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int Miktar { get; set; }
    public int MinStok { get; set; }
    public int MaxStok { get; set; }
    public string Depo { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsAlarm => Miktar < MinStok;

    /// <summary>EMR-06 renk tablosu: 4 seviye stok durumu.</summary>
    public string StokDurum
    {
        get
        {
            if (Miktar <= 0) return "TUKENDI";
            if (Miktar <= MinStok) return "KRITIK";
            if (Miktar <= MinStok * 1.5) return "DUSUK";
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
