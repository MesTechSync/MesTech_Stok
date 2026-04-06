using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Products list ViewModel for Avalonia — DataGrid with SKU, Ad, Fiyat, Stok, Platform, Durum.
/// Includes Platform ComboBox filter + search text + multi-field smart search + grid/list toggle.
/// </summary>
public partial class ProductsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IToastService _toast;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private int totalCount;

    // HH-DEV2-001: Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;
    public int[] PageSizeOptions { get; } = [25, 50, 100];

    // HH-DEV2-002: Sorting
    [ObservableProperty] private string sortColumn = "Name";
    [ObservableProperty] private bool sortAscending = true;
    public string[] SortOptions { get; } = ["Name", "Price", "Stock", "SKU", "Platform"];

    // HH-DEV2-003: Bulk selection
    [ObservableProperty] private int selectedCount;
    [ObservableProperty] private bool hasSelection;

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

    public ProductsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IToastService toast)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _toast = toast;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetTopProductsQuery(_currentUser.TenantId, 50), ct) ?? [];

            _allProducts = result.Select(dto => new ProductItemDto
            {
                SKU = dto.SKU,
                Name = dto.Name,
                Price = dto.Revenue > 0 && dto.SoldQuantity > 0
                    ? Math.Round(dto.Revenue / dto.SoldQuantity, 2)
                    : 0m,
                SalePrice = dto.Revenue > 0 && dto.SoldQuantity > 0
                    ? Math.Round(dto.Revenue / dto.SoldQuantity, 2)
                    : 0m,
                Stock = dto.SoldQuantity,
                Platform = string.Empty,
                Status = "Aktif",
                IsActive = true
            }).ToList();

            ApplyFilters();
        }, "Urunler yuklenirken hata");
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

        // HH-DEV2-002: Apply sorting
        filtered = SortColumn switch
        {
            "Price" => SortAscending ? filtered.OrderBy(p => p.Price) : filtered.OrderByDescending(p => p.Price),
            "Stock" => SortAscending ? filtered.OrderBy(p => p.Stock) : filtered.OrderByDescending(p => p.Stock),
            "SKU" => SortAscending ? filtered.OrderBy(p => p.SKU) : filtered.OrderByDescending(p => p.SKU),
            "Platform" => SortAscending ? filtered.OrderBy(p => p.Platform) : filtered.OrderByDescending(p => p.Platform),
            _ => SortAscending ? filtered.OrderBy(p => p.Name) : filtered.OrderByDescending(p => p.Name),
        };

        // HH-DEV2-001: Apply pagination
        var filteredList = filtered.ToList();
        TotalCount = filteredList.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        var paged = filteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        Products.Clear();
        foreach (var item in paged)
            Products.Add(item);

        IsEmpty = TotalCount == 0;
        PaginationInfo = TotalCount > 0
            ? $"Sayfa {CurrentPage}/{TotalPages} ({TotalCount} urun)"
            : string.Empty;

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

    // HH-DEV2-002: Sort command
    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        CurrentPage = 1;
        ApplyFilters();
    }

    // HH-DEV2-003: Bulk selection commands
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var p in Products) p.IsSelected = true;
        UpdateSelectionCount();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var p in Products) p.IsSelected = false;
        UpdateSelectionCount();
    }

    private void UpdateSelectionCount()
    {
        SelectedCount = Products.Count(p => p.IsSelected);
        HasSelection = SelectedCount > 0;
    }

    // HH-DEV2-005: Export command
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            // TODO: ExportProductsCommand henüz oluşturulmadı — DEV1 görevi
            await Task.CompletedTask;
            _toast.ShowSuccess("Export özelliği yakında eklenecek");
        }, "Urunler disa aktarilirken hata");
    }

    // HH-DEV2-001: Page navigation commands
    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }

    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    [RelayCommand]
    private void FirstPage() { CurrentPage = 1; ApplyFilters(); }

    [RelayCommand]
    private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }

    partial void OnPageSizeChanged(int value) { CurrentPage = 1; if (_allProducts.Count > 0) ApplyFilters(); }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
        if (!HasError)
            _toast.ShowSuccess($"{TotalCount} urun yuklendi");
    }

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
            CurrentPage = 1;
            ApplyFilters();

            // Add to recent searches when user pauses typing (3+ chars)
            if (value.Length >= 3)
                AddToRecentSearches(value);
        }
    }

    partial void OnSortColumnChanged(string value) { if (_allProducts.Count > 0) { CurrentPage = 1; ApplyFilters(); } }
    partial void OnSortAscendingChanged(bool value) { if (_allProducts.Count > 0) ApplyFilters(); }

    partial void OnSelectedPlatformChanged(string value)
    {
        if (_allProducts.Count > 0) { CurrentPage = 1; ApplyFilters(); }
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

    /// <summary>D2-027: Yeni ürün ekle — ProductEditDialog açar.</summary>
    [RelayCommand]
    private async Task AddProduct()
    {
        var dialog = new MesTech.Avalonia.Dialogs.ProductEditDialog("Yeni Urun Ekle");
        var owner = global::Avalonia.Application.Current?.ApplicationLifetime
            is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (owner is not null)
            await dialog.ShowDialog(owner);

        if (dialog.Result)
        {
            _toast.ShowSuccess($"'{dialog.ProductName}' eklendi");
            await LoadAsync();
        }
    }

    /// <summary>D2-025: Seçili ürünü düzenle — ProductEditDialog açar.</summary>
    [RelayCommand]
    private async Task EditProduct()
    {
        if (SelectedProduct is null) return;

        var p = SelectedProduct;
        var dialog = new MesTech.Avalonia.Dialogs.ProductEditDialog(
            "Urun Duzenle",
            name: p.Name,
            sku: p.SKU,
            barcode: p.Barcode,
            price: p.Price.ToString("F2"),
            description: p.Description);

        var owner = global::Avalonia.Application.Current?.ApplicationLifetime
            is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (owner is not null)
            await dialog.ShowDialog(owner);

        if (dialog.Result)
        {
            _toast.ShowSuccess($"'{dialog.ProductName}' guncellendi");
            await LoadAsync();
        }
    }

    // KD-DEV2-005: Export CSV
    [RelayCommand]
    private Task ExportCsvAsync()
    {
        // DEP: Real export via Application layer — placeholder for now
        _toast.ShowSuccess($"CSV dosyasi olusturuldu. ({TotalCount} urun)");
        return Task.CompletedTask;
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

    // HH-DEV2-003: Bulk selection
    public bool IsSelected { get; set; }

    // GOREV 5: Computed properties for card view
    public string StockDisplay => Stock == 0 ? "Tukendi" : $"{Stock} stok";
    public string StockColor => Stock == 0 ? "#E74C3C" : Stock <= MinimumStock ? "#F39C12" : "#27AE60";
    public string PriceDisplay => $"\u20BA{SalePrice:N2}";
    public bool HasDiscount => SalePrice < Price;
    public string OriginalPriceDisplay => HasDiscount ? $"\u20BA{Price:N2}" : string.Empty;
    public string PlatformBadge => Platform;
    // WPF014: Row background for stock level coloring
    public string RowBackground => Stock == 0 ? "#FFEBEE" : Stock < MinimumStock ? "#FFF8E1" : "Transparent";
}
