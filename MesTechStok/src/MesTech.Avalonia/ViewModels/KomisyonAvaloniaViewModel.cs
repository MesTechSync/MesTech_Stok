using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class KomisyonAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI — Platform averages
    [ObservableProperty] private string trendyolAvgRate = "%0.0";
    [ObservableProperty] private string hepsiburadaAvgRate = "%0.0";
    [ObservableProperty] private string ciceksepetiAvgRate = "%0.0";
    [ObservableProperty] private string n11AvgRate = "%0.0";

    // Filters
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedCategory = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<CommissionItemDto> Items { get; } = [];
    private List<CommissionItemDto> _allItems = [];

    public ObservableCollection<string> Platforms { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "Ciceksepeti", "N11", "Amazon", "Pazarama"];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Giyim", "Elektronik", "Ev & Yasam", "Kozmetik", "Gida", "Diger"];

    public KomisyonAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(200); // Will be replaced with MediatR query

            TrendyolAvgRate = "%12.5";
            HepsiburadaAvgRate = "%15.0";
            CiceksepetiAvgRate = "%18.0";
            N11AvgRate = "%11.0";

            _allItems =
            [
                new() { Platform = "Trendyol", Category = "Giyim", RateFormatted = "%12.0", FixedFeeFormatted = "0,00 TL", ValidFrom = "01.01.2026" },
                new() { Platform = "Trendyol", Category = "Elektronik", RateFormatted = "%9.5", FixedFeeFormatted = "0,00 TL", ValidFrom = "01.01.2026" },
                new() { Platform = "Hepsiburada", Category = "Giyim", RateFormatted = "%15.0", FixedFeeFormatted = "3,00 TL", ValidFrom = "01.01.2026" },
                new() { Platform = "Hepsiburada", Category = "Elektronik", RateFormatted = "%11.0", FixedFeeFormatted = "3,00 TL", ValidFrom = "01.01.2026" },
                new() { Platform = "Ciceksepeti", Category = "Kozmetik", RateFormatted = "%18.0", FixedFeeFormatted = "5,00 TL", ValidFrom = "15.02.2026" },
                new() { Platform = "N11", Category = "Ev & Yasam", RateFormatted = "%11.0", FixedFeeFormatted = "2,50 TL", ValidFrom = "01.03.2026" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Komisyon verileri yuklenemedi: {ex.Message}";
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
            filtered = filtered.Where(x =>
                x.Platform.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedPlatform != "Tumu")
            filtered = filtered.Where(x => x.Platform == SelectedPlatform);

        if (SelectedCategory != "Tumu")
            filtered = filtered.Where(x => x.Category == SelectedCategory);

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddCommission()
    {
        // Will open add commission dialog
    }
}

public class CommissionItemDto
{
    public string Platform { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RateFormatted { get; set; } = string.Empty;
    public string FixedFeeFormatted { get; set; } = string.Empty;
    public string ValidFrom { get; set; } = string.Empty;
}
