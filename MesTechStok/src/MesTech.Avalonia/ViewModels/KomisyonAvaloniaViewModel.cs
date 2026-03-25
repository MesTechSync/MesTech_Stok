using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;

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
            var result = await _mediator.Send(new GetCommissionSummaryQuery(
                Guid.Empty, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow));

            _allItems = result.ByPlatform.Select(p => new CommissionItemDto
            {
                Platform = p.Platform,
                Category = "-",
                RateFormatted = $"%{p.AverageRate:F1}",
                FixedFeeFormatted = $"{p.TotalCommission:N2} TL",
                ValidFrom = DateTime.UtcNow.ToString("dd.MM.yyyy")
            }).ToList();

            var lookup = result.ByPlatform.ToDictionary(p => p.Platform.ToLowerInvariant(), p => p.AverageRate);
            TrendyolAvgRate = lookup.TryGetValue("trendyol", out var tr) ? $"%{tr:F1}" : "%0.0";
            HepsiburadaAvgRate = lookup.TryGetValue("hepsiburada", out var hb) ? $"%{hb:F1}" : "%0.0";
            CiceksepetiAvgRate = lookup.TryGetValue("ciceksepeti", out var cs) ? $"%{cs:F1}" : "%0.0";
            N11AvgRate = lookup.TryGetValue("n11", out var n11) ? $"%{n11:F1}" : "%0.0";

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
