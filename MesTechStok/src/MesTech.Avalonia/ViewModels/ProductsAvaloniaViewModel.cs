using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Products list ViewModel for Avalonia — DataGrid with SKU, Ad, Fiyat, Stok, Platform, Durum.
/// Includes Platform ComboBox filter + search text + multi-field smart search + grid/list toggle.
/// </summary>
public partial class ProductsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private int totalCount;

    // GOREV 5: Grid/List toggle
    [ObservableProperty] private bool isGridView;
    [ObservableProperty] private bool isListView = true;

    // GOREV 4: Smart search
    [ObservableProperty] private string searchMatchInfo = string.Empty;
    [ObservableProperty] private bool filterOutOfStock;
    [ObservableProperty] private bool filterLowStock;
    [ObservableProperty] private bool filterDiscounted;
    [ObservableProperty] private ProductItemDto? selectedProduct;

    public ObservableCollection<ProductItemDto> Products { get; } = [];

    public ObservableCollection<string> Platforms { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "OpenCart"
    ];

    public ObservableCollection<string> RecentSearches { get; } = [];

    private List<ProductItemDto> _allProducts = [];

    public ProductsAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(300); // Simulate async load

            _allProducts =
            [
                new() { SKU = "TRY-ELK-001", Name = "Samsung Galaxy S24 Ultra", Price = 64999.00m, Stock = 45, Platform = "Trendyol", Status = "Aktif", Barcode = "8806095380001", Description = "256GB, Titanium Violet", ImageUrl = "", MinimumStock = 10, IsActive = true, SalePrice = 59999.00m, VariantSKU = "TRY-ELK-001-VLT", VariantBarcode = "8806095380002" },
                new() { SKU = "HB-BLG-002", Name = "Apple MacBook Air M3", Price = 54999.00m, Stock = 12, Platform = "Hepsiburada", Status = "Aktif", Barcode = "0194253943518", Description = "15 inc, 16GB RAM, 512GB SSD", ImageUrl = "", MinimumStock = 5, IsActive = true, SalePrice = 54999.00m, VariantSKU = "HB-BLG-002-GRY", VariantBarcode = "0194253943525" },
                new() { SKU = "N11-AKS-003", Name = "Sony WH-1000XM5 Kulaklik", Price = 11499.00m, Stock = 78, Platform = "N11", Status = "Aktif", Barcode = "4548736132610", Description = "Kablosuz, Gurultu Onleyici, 30 saat pil", ImageUrl = "", MinimumStock = 15, IsActive = true, SalePrice = 9999.00m, VariantSKU = "", VariantBarcode = "" },
                new() { SKU = "TRY-AKS-004", Name = "Logitech MX Master 3S Mouse", Price = 3299.00m, Stock = 156, Platform = "Trendyol", Status = "Aktif", Barcode = "5099206101203", Description = "Ergonomik, 8K DPI, USB-C", ImageUrl = "", MinimumStock = 20, IsActive = true, SalePrice = 3299.00m, VariantSKU = "TRY-AKS-004-BLK", VariantBarcode = "5099206101210" },
                new() { SKU = "CS-MNT-005", Name = "Dell U2723QE 4K Monitor", Price = 18799.00m, Stock = 8, Platform = "Ciceksepeti", Status = "Dusuk Stok", Barcode = "5397184505106", Description = "27 inc IPS, USB-C Hub, HDR400", ImageUrl = "", MinimumStock = 10, IsActive = true, SalePrice = 16999.00m, VariantSKU = "", VariantBarcode = "" },
                new() { SKU = "AMZ-GYM-006", Name = "Dyson V15 Detect Supurge", Price = 28990.00m, Stock = 23, Platform = "Amazon", Status = "Aktif", Barcode = "5025155072530", Description = "Lazer toz algilama, LCD ekran", ImageUrl = "", MinimumStock = 5, IsActive = true, SalePrice = 28990.00m, VariantSKU = "", VariantBarcode = "" },
                new() { SKU = "OC-EV-007", Name = "Philips Airfryer XXL", Price = 7499.00m, Stock = 0, Platform = "OpenCart", Status = "Tukendi", Barcode = "8710103940951", Description = "7.3L, Dijital, RapidAir", ImageUrl = "", MinimumStock = 10, IsActive = false, SalePrice = 6499.00m, VariantSKU = "", VariantBarcode = "" },
                new() { SKU = "TRY-GYM-008", Name = "Karaca Hatir Turk Kahve Makinesi", Price = 2199.00m, Stock = 340, Platform = "Trendyol", Status = "Aktif", Barcode = "8680214001234", Description = "5 fincan, Turk kahvesi, sut kopu", ImageUrl = "", MinimumStock = 30, IsActive = true, SalePrice = 2199.00m, VariantSKU = "TRY-GYM-008-RED", VariantBarcode = "8680214001241" },
                new() { SKU = "HB-KSA-009", Name = "Vestel 55 inc 4K Smart TV", Price = 22499.00m, Stock = 5, Platform = "Hepsiburada", Status = "Dusuk Stok", Barcode = "8698902183456", Description = "VIDAA, Dolby Vision, HDR10+", ImageUrl = "", MinimumStock = 8, IsActive = true, SalePrice = 19999.00m, VariantSKU = "", VariantBarcode = "" },
                new() { SKU = "N11-SPR-010", Name = "Nike Air Max 270 Ayakkabi", Price = 4599.00m, Stock = 67, Platform = "N11", Status = "Aktif", Barcode = "0193654692810", Description = "Erkek, Siyah/Beyaz, Air yastik", ImageUrl = "", MinimumStock = 20, IsActive = true, SalePrice = 3999.00m, VariantSKU = "N11-SPR-010-42", VariantBarcode = "0193654692827" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Urunler yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();
        string matchField = string.Empty;

        // GOREV 4: Multi-field smart search
        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            var matchResults = new List<(ProductItemDto Product, string MatchField)>();

            foreach (var p in filtered)
            {
                if (p.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase))
                    matchResults.Add((p, "Barkod"));
                else if (p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase))
                    matchResults.Add((p, "SKU"));
                else if (p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    matchResults.Add((p, "Ad"));
                else if (p.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                    matchResults.Add((p, "Aciklama"));
                else if (!string.IsNullOrEmpty(p.VariantSKU) && p.VariantSKU.Contains(search, StringComparison.OrdinalIgnoreCase))
                    matchResults.Add((p, "Varyant SKU"));
                else if (!string.IsNullOrEmpty(p.VariantBarcode) && p.VariantBarcode.Contains(search, StringComparison.OrdinalIgnoreCase))
                    matchResults.Add((p, "Varyant Barkod"));
            }

            filtered = matchResults.Select(m => m.Product);

            if (matchResults.Count == 1)
            {
                matchField = matchResults[0].MatchField;
                // Auto-select on single barcode match
                if (matchField == "Barkod" || matchField == "Varyant Barkod")
                    SelectedProduct = matchResults[0].Product;
            }
            else if (matchResults.Count > 1)
            {
                var fieldGroups = matchResults.Select(m => m.MatchField).Distinct().ToList();
                matchField = string.Join(", ", fieldGroups);
            }
        }

        // GOREV 4: Quick filter chips
        if (FilterOutOfStock)
            filtered = filtered.Where(p => p.Stock == 0);

        if (FilterLowStock)
            filtered = filtered.Where(p => p.Stock > 0 && p.Stock <= p.MinimumStock);

        if (FilterDiscounted)
            filtered = filtered.Where(p => p.SalePrice < p.Price);

        if (SelectedPlatform != "Tumu")
        {
            filtered = filtered.Where(p => p.Platform == SelectedPlatform);
        }

        Products.Clear();
        foreach (var item in filtered)
            Products.Add(item);

        TotalCount = Products.Count;
        IsEmpty = Products.Count == 0;

        // Update match info
        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2 && TotalCount > 0)
            SearchMatchInfo = $"{TotalCount} sonuc ({matchField})";
        else
            SearchMatchInfo = string.Empty;
    }

    private void AddToRecentSearches(string search)
    {
        if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
            return;

        // Remove if already exists
        if (RecentSearches.Contains(search))
            RecentSearches.Remove(search);

        // Add to front
        RecentSearches.Insert(0, search);

        // Keep last 5
        while (RecentSearches.Count > 5)
            RecentSearches.RemoveAt(RecentSearches.Count - 1);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    // GOREV 5: Toggle view command
    [RelayCommand]
    private void ToggleView()
    {
        IsGridView = !IsGridView;
        IsListView = !IsGridView;
    }

    [RelayCommand]
    private void SetGridView()
    {
        IsGridView = true;
        IsListView = false;
    }

    [RelayCommand]
    private void SetListView()
    {
        IsGridView = false;
        IsListView = true;
    }

    // GOREV 4: Barcode quick-search
    [RelayCommand]
    private void BarcodeSearch()
    {
        // Focus-mode: treated as barcode lookup when triggered
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            AddToRecentSearches(SearchText);
            ApplyFilters();
        }
    }

    // GOREV 4: Apply recent search
    [RelayCommand]
    private void ApplyRecentSearch(string search)
    {
        SearchText = search;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allProducts.Count > 0)
        {
            ApplyFilters();

            // Add to recent searches when user pauses typing (3+ chars)
            if (value.Length >= 3)
                AddToRecentSearches(value);
        }
    }

    partial void OnSelectedPlatformChanged(string value)
    {
        if (_allProducts.Count > 0)
            ApplyFilters();
    }

    partial void OnFilterOutOfStockChanged(bool value)
    {
        if (_allProducts.Count > 0)
            ApplyFilters();
    }

    partial void OnFilterLowStockChanged(bool value)
    {
        if (_allProducts.Count > 0)
            ApplyFilters();
    }

    partial void OnFilterDiscountedChanged(bool value)
    {
        if (_allProducts.Count > 0)
            ApplyFilters();
    }
}

public class ProductItemDto
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // GOREV 4: Additional fields for smart search
    public string Barcode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal SalePrice { get; set; }
    public string VariantSKU { get; set; } = string.Empty;
    public string VariantBarcode { get; set; } = string.Empty;

    // GOREV 5: Computed properties for card view
    public string StockDisplay => Stock == 0 ? "Tukendi" : $"{Stock} stok";
    public string StockColor => Stock == 0 ? "#E74C3C" : Stock <= MinimumStock ? "#F39C12" : "#27AE60";
    public string PriceDisplay => $"\u20BA{SalePrice:N2}";
    public bool HasDiscount => SalePrice < Price;
    public string OriginalPriceDisplay => HasDiscount ? $"\u20BA{Price:N2}" : string.Empty;
    public string PlatformBadge => Platform;
}
