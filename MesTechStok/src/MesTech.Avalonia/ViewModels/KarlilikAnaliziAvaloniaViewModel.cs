using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class KarlilikAnaliziAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI
    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalProfit = "0,00 TL";
    [ObservableProperty] private string averageMargin = "%0.0";

    // Filters
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedCategory = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;

    public ObservableCollection<ProfitabilityItemDto> Items { get; } = [];
    private List<ProfitabilityItemDto> _allItems = [];

    public ObservableCollection<string> Platforms { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon"];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Giyim", "Elektronik", "Ev & Yasam", "Kozmetik", "Gida"];

    public KarlilikAnaliziAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(200); // Will be replaced with MediatR query

            _allItems =
            [
                new() { ProductName = "Erkek Gomlek - Beyaz XL", SalesFormatted = "24.500,00 TL", CostFormatted = "12.250,00 TL", CommissionFormatted = "2.940,00 TL", ShippingFormatted = "1.470,00 TL", NetProfitFormatted = "7.840,00 TL", MarginFormatted = "%32.0", NetProfit = 7840 },
                new() { ProductName = "Kadin Canta - Siyah", SalesFormatted = "18.200,00 TL", CostFormatted = "7.280,00 TL", CommissionFormatted = "2.730,00 TL", ShippingFormatted = "910,00 TL", NetProfitFormatted = "7.280,00 TL", MarginFormatted = "%40.0", NetProfit = 7280 },
                new() { ProductName = "Bluetooth Kulaklik Pro", SalesFormatted = "15.600,00 TL", CostFormatted = "9.360,00 TL", CommissionFormatted = "1.482,00 TL", ShippingFormatted = "780,00 TL", NetProfitFormatted = "3.978,00 TL", MarginFormatted = "%25.5", NetProfit = 3978 },
                new() { ProductName = "Organik Cay Seti", SalesFormatted = "8.400,00 TL", CostFormatted = "3.360,00 TL", CommissionFormatted = "1.008,00 TL", ShippingFormatted = "840,00 TL", NetProfitFormatted = "3.192,00 TL", MarginFormatted = "%38.0", NetProfit = 3192 },
                new() { ProductName = "Ev Dekorasyon Seti", SalesFormatted = "6.800,00 TL", CostFormatted = "4.080,00 TL", CommissionFormatted = "1.020,00 TL", ShippingFormatted = "680,00 TL", NetProfitFormatted = "1.020,00 TL", MarginFormatted = "%15.0", NetProfit = 1020 },
            ];

            var revenue = 73500m;
            var profit = _allItems.Sum(x => x.NetProfit);
            var margin = revenue > 0 ? profit / revenue * 100 : 0;

            TotalRevenue = $"{revenue:N2} TL";
            TotalProfit = $"{profit:N2} TL";
            AverageMargin = $"%{margin:N1}";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Karlilik analizi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedPlatformChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ProfitabilityItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SalesFormatted { get; set; } = string.Empty;
    public string CostFormatted { get; set; } = string.Empty;
    public string CommissionFormatted { get; set; } = string.Empty;
    public string ShippingFormatted { get; set; } = string.Empty;
    public string NetProfitFormatted { get; set; } = string.Empty;
    public string MarginFormatted { get; set; } = string.Empty;
    public decimal NetProfit { get; set; }
}
