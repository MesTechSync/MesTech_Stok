using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fulfillment cross-inventory comparison ViewModel — F-03.
/// DataGrid: SKU, ProductName, FBA Qty, HepsiL Qty, Local Qty, Total, Status (color-coded).
/// Tab per provider. Uses GetFulfillmentInventoryQuery per center.
/// </summary>
public partial class FulfillmentInventoryViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedTab = "Tumu";
    [ObservableProperty] private int totalCount;

    // Tab font weights for visual active state
    [ObservableProperty] private string tabAllWeight = "Bold";
    [ObservableProperty] private string tabFbaWeight = "Normal";
    [ObservableProperty] private string tabHepsiWeight = "Normal";
    [ObservableProperty] private string tabLocalWeight = "Normal";

    public ObservableCollection<InventoryComparisonDto> InventoryItems { get; } = [];

    private List<InventoryComparisonDto> _allItems = [];

    public FulfillmentInventoryViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var fbaInventory = await _mediator.Send(new GetFulfillmentInventoryQuery(FulfillmentCenter.AmazonFBA, Array.Empty<string>()));
            var hepsiInventory = await _mediator.Send(new GetFulfillmentInventoryQuery(FulfillmentCenter.Hepsilojistik, Array.Empty<string>()));
            var localInventory = await _mediator.Send(new GetFulfillmentInventoryQuery(FulfillmentCenter.OwnWarehouse, Array.Empty<string>()));

            // Merge by SKU
            var skuSet = new HashSet<string>();
            var fbaMap = new Dictionary<string, int>();
            var hepsiMap = new Dictionary<string, int>();
            var localMap = new Dictionary<string, int>();

            foreach (var stock in fbaInventory.Stocks)
            {
                skuSet.Add(stock.SKU);
                fbaMap[stock.SKU] = stock.AvailableQuantity;
            }

            foreach (var stock in hepsiInventory.Stocks)
            {
                skuSet.Add(stock.SKU);
                hepsiMap[stock.SKU] = stock.AvailableQuantity;
            }

            foreach (var stock in localInventory.Stocks)
            {
                skuSet.Add(stock.SKU);
                localMap[stock.SKU] = stock.AvailableQuantity;
            }

            _allItems = skuSet.Select(sku =>
            {
                var fba = fbaMap.GetValueOrDefault(sku, 0);
                var hepsi = hepsiMap.GetValueOrDefault(sku, 0);
                var local = localMap.GetValueOrDefault(sku, 0);
                var total = fba + hepsi + local;

                return new InventoryComparisonDto
                {
                    Sku = sku,
                    ProductName = sku, // Will be enriched from product catalog
                    FbaQty = fba,
                    HepsiQty = hepsi,
                    LocalQty = local,
                    TotalQty = total,
                    Status = total == 0 ? "Stok Yok" : total < 5 ? "Kritik" : "Yeterli"
                };
            }).OrderBy(i => i.Sku).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Envanter verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Tab filter: show only items that have stock in selected center
        filtered = SelectedTab switch
        {
            "AmazonFBA" => filtered.Where(i => i.FbaQty > 0),
            "Hepsilojistik" => filtered.Where(i => i.HepsiQty > 0),
            "OwnWarehouse" => filtered.Where(i => i.LocalQty > 0),
            _ => filtered
        };

        InventoryItems.Clear();
        foreach (var item in filtered)
            InventoryItems.Add(item);

        TotalCount = InventoryItems.Count;
        IsEmpty = InventoryItems.Count == 0 && !HasError;
    }

    private void UpdateTabWeights()
    {
        TabAllWeight = SelectedTab == "Tumu" ? "Bold" : "Normal";
        TabFbaWeight = SelectedTab == "AmazonFBA" ? "Bold" : "Normal";
        TabHepsiWeight = SelectedTab == "Hepsilojistik" ? "Bold" : "Normal";
        TabLocalWeight = SelectedTab == "OwnWarehouse" ? "Bold" : "Normal";
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void SelectTab(string tab)
    {
        SelectedTab = tab;
        UpdateTabWeights();
        if (_allItems.Count > 0)
            ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allItems.Count > 0)
            ApplyFilters();
    }
}

public class InventoryComparisonDto
{
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int FbaQty { get; set; }
    public int HepsiQty { get; set; }
    public int LocalQty { get; set; }
    public int TotalQty { get; set; }
    public string Status { get; set; } = string.Empty;
}
