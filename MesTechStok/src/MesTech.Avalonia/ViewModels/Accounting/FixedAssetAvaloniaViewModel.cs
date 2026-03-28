using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class FixedAssetAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalAssetValue = "0,00 TL";
    [ObservableProperty] private string totalDepreciation = "0,00 TL";
    [ObservableProperty] private string netBookValue = "0,00 TL";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedCategory;

    public ObservableCollection<FixedAssetItemDto> Items { get; } = [];
    private List<FixedAssetItemDto> _allItems = [];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Bilgisayar", "Mobilya", "Arac", "Makine", "Yazilim", "Diger"];

    public FixedAssetAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        SelectedCategory = "Tumu";
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            _allItems =
            [
                new() { Name = "MacBook Pro 16\" M3", Category = "Bilgisayar", PurchaseDate = "2026-01-15", PurchaseValue = 85000m, PurchaseValueFormatted = "85.000,00 TL", DepreciationFormatted = "7.083,33 TL", NetValueFormatted = "77.916,67 TL", UsefulLifeYears = 3, IsActive = true, StatusText = "Aktif" },
                new() { Name = "Ofis Masasi Set (4 adet)", Category = "Mobilya", PurchaseDate = "2025-06-01", PurchaseValue = 32000m, PurchaseValueFormatted = "32.000,00 TL", DepreciationFormatted = "10.666,67 TL", NetValueFormatted = "21.333,33 TL", UsefulLifeYears = 5, IsActive = true, StatusText = "Aktif" },
                new() { Name = "Dell Monitor 27\" (3 adet)", Category = "Bilgisayar", PurchaseDate = "2025-09-10", PurchaseValue = 27000m, PurchaseValueFormatted = "27.000,00 TL", DepreciationFormatted = "4.500,00 TL", NetValueFormatted = "22.500,00 TL", UsefulLifeYears = 3, IsActive = true, StatusText = "Aktif" },
                new() { Name = "MesTech ERP Lisansi", Category = "Yazilim", PurchaseDate = "2026-01-01", PurchaseValue = 45000m, PurchaseValueFormatted = "45.000,00 TL", DepreciationFormatted = "3.750,00 TL", NetValueFormatted = "41.250,00 TL", UsefulLifeYears = 3, IsActive = true, StatusText = "Aktif" },
                new() { Name = "Eski Yazici (HP LaserJet)", Category = "Bilgisayar", PurchaseDate = "2022-03-01", PurchaseValue = 8500m, PurchaseValueFormatted = "8.500,00 TL", DepreciationFormatted = "8.500,00 TL", NetValueFormatted = "0,00 TL", UsefulLifeYears = 3, IsActive = false, StatusText = "Pasif" },
            ];

            var totalValue = _allItems.Sum(x => x.PurchaseValue);
            var totalDepr = _allItems.Sum(x => decimal.Parse(x.DepreciationFormatted.Replace(".", "").Replace(",", ".").Replace(" TL", ""), System.Globalization.CultureInfo.InvariantCulture));
            var netBook = totalValue - totalDepr;

            TotalAssetValue = $"{totalValue:N2} TL";
            TotalDepreciation = $"{totalDepr:N2} TL";
            NetBookValue = $"{netBook:N2} TL";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Sabit kiymet verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string? value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "Tumu")
        {
            filtered = filtered.Where(x => x.Category == SelectedCategory);
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class FixedAssetItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PurchaseDate { get; set; } = string.Empty;
    public decimal PurchaseValue { get; set; }
    public string PurchaseValueFormatted { get; set; } = string.Empty;
    public string DepreciationFormatted { get; set; } = string.Empty;
    public string NetValueFormatted { get; set; } = string.Empty;
    public int UsefulLifeYears { get; set; }
    public bool IsActive { get; set; }
    public string StatusText { get; set; } = string.Empty;
}
