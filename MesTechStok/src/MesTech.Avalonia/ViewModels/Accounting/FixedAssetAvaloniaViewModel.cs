using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetFixedAssets;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class FixedAssetAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

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

    public FixedAssetAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        SelectedCategory = "Tumu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetFixedAssetsQuery(_currentUser.TenantId), ct) ?? [];

            _allItems = result.Select(a => new FixedAssetItemDto
            {
                Name = a.Name,
                Category = !string.IsNullOrEmpty(a.AssetCode) ? a.AssetCode : "Diger",
                PurchaseDate = a.AcquisitionDate.ToString("yyyy-MM-dd"),
                PurchaseValue = a.AcquisitionCost,
                PurchaseValueFormatted = $"{a.AcquisitionCost:N2} TL",
                DepreciationFormatted = $"{a.AccumulatedDepreciation:N2} TL",
                NetValueFormatted = $"{a.NetBookValue:N2} TL",
                UsefulLifeYears = a.UsefulLifeYears,
                IsActive = a.IsActive,
                StatusText = a.IsActive ? "Aktif" : "Pasif"
            }).ToList();

            var totalValue = _allItems.Sum(x => x.PurchaseValue);
            var totalDepr = result.Sum(x => x.AccumulatedDepreciation);
            var netBook = result.Sum(x => x.NetBookValue);

            TotalAssetValue = $"{totalValue:N2} TL";
            TotalDepreciation = $"{totalDepr:N2} TL";
            NetBookValue = $"{netBook:N2} TL";

            ApplyFilters();
        }, "Sabit varliklar yuklenirken hata");
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
