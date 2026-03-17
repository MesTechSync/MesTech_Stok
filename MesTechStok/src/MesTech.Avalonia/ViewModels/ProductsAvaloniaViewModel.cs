using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Products list ViewModel for Avalonia — DataGrid with SKU, Ad, Fiyat, Stok, Platform, Durum.
/// Includes Platform ComboBox filter + search text.
/// </summary>
public partial class ProductsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ProductItemDto> Products { get; } = [];

    public ObservableCollection<string> Platforms { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "OpenCart"
    ];

    private List<ProductItemDto> _allProducts = [];

    public ProductsAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(300); // Simulate async load

            _allProducts =
            [
                new() { SKU = "TRY-ELK-001", Name = "Samsung Galaxy S24 Ultra", Price = 64999.00m, Stock = 45, Platform = "Trendyol", Status = "Aktif" },
                new() { SKU = "HB-BLG-002", Name = "Apple MacBook Air M3", Price = 54999.00m, Stock = 12, Platform = "Hepsiburada", Status = "Aktif" },
                new() { SKU = "N11-AKS-003", Name = "Sony WH-1000XM5 Kulaklik", Price = 11499.00m, Stock = 78, Platform = "N11", Status = "Aktif" },
                new() { SKU = "TRY-AKS-004", Name = "Logitech MX Master 3S Mouse", Price = 3299.00m, Stock = 156, Platform = "Trendyol", Status = "Aktif" },
                new() { SKU = "CS-MNT-005", Name = "Dell U2723QE 4K Monitor", Price = 18799.00m, Stock = 8, Platform = "Ciceksepeti", Status = "Dusuk Stok" },
                new() { SKU = "AMZ-GYM-006", Name = "Dyson V15 Detect Supurge", Price = 28990.00m, Stock = 23, Platform = "Amazon", Status = "Aktif" },
                new() { SKU = "OC-EV-007", Name = "Philips Airfryer XXL", Price = 7499.00m, Stock = 0, Platform = "OpenCart", Status = "Tukendi" },
                new() { SKU = "TRY-GYM-008", Name = "Karaca Hatir Turk Kahve Makinesi", Price = 2199.00m, Stock = 340, Platform = "Trendyol", Status = "Aktif" },
                new() { SKU = "HB-KSA-009", Name = "Vestel 55 inc 4K Smart TV", Price = 22499.00m, Stock = 5, Platform = "Hepsiburada", Status = "Dusuk Stok" },
                new() { SKU = "N11-SPR-010", Name = "Nike Air Max 270 Ayakkabi", Price = 4599.00m, Stock = 67, Platform = "N11", Status = "Aktif" },
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

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedPlatform != "Tumu")
        {
            filtered = filtered.Where(p => p.Platform == SelectedPlatform);
        }

        Products.Clear();
        foreach (var item in filtered)
            Products.Add(item);

        TotalCount = Products.Count;
        IsEmpty = Products.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allProducts.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedPlatformChanged(string value)
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
}
