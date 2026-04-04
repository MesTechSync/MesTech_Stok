using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockPlacements;
using MesTech.Domain.Interfaces;

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

    private readonly ICurrentUserService _currentUser;

    public StockPlacementAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var placements = await _mediator.Send(new GetStockPlacementsQuery(_currentUser.TenantId), ct) ?? [];

            Warehouses.Clear();
            var warehouseNames = placements.Select(p => p.WarehouseName ?? "—").Distinct().OrderBy(w => w);
            foreach (var w in warehouseNames) Warehouses.Add(w);

            _allItems = placements.Select(p => new PlacementItemDto
            {
                Sku = p.ProductSku ?? "—",
                Ad = p.ProductName ?? "—",
                Miktar = p.Quantity,
                MinimumStock = p.MinimumStock,
                Depo = p.WarehouseName ?? "—",
                Raf = p.ShelfCode ?? p.BinCode ?? "—"
            }).ToList();

            if (SelectedWarehouse is null && Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];

            ApplyFilters();
        }, "Yerlesim verileri yuklenirken hata");
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
