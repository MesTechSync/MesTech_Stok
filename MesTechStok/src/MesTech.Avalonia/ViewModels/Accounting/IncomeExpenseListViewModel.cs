using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Gelir/Gider Liste ViewModel — IE-02.
/// Filtreleme (Tur, Kategori, Tarih, Arama) + Sayfalama + Excel Export.
/// </summary>
public partial class IncomeExpenseListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Filters
    [ObservableProperty] private string _selectedTypeFilter = "Tumu";
    [ObservableProperty] private string _selectedCategory = "Tumu";
    [ObservableProperty] private DateTimeOffset? _dateFrom;
    [ObservableProperty] private DateTimeOffset? _dateTo;
    [ObservableProperty] private string _searchText = string.Empty;

    // Pagination
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;

    [ObservableProperty] private bool _isEmpty;

    private const int PageSize = 20;
    private List<IncomeExpenseListItemDto> _allItems = [];

    public ObservableCollection<IncomeExpenseListItemDto> Items { get; } = [];

    public ObservableCollection<string> TypeFilters { get; } =
        ["Tumu", "Gelir", "Gider"];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Satis", "Kargo", "Genel Gider", "Komisyon", "Iade", "Hizmet", "Faiz", "Diger"];

    public IncomeExpenseListViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Gelir / Gider Listesi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            await Task.Delay(200, ct); // Will be replaced with MediatR query

            _allItems =
            [
                new() { Date = "24.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+8.240,00 TL", Amount = 8240m, Platform = "Trendyol", Description = "Trendyol satis hasilati" },
                new() { Date = "23.03.2026", Type = "Gider", Category = "Kargo", AmountFormatted = "-620,00 TL", Amount = -620m, Platform = "Aras Kargo", Description = "Kargo gideri" },
                new() { Date = "23.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+3.180,00 TL", Amount = 3180m, Platform = "Hepsiburada", Description = "Hepsiburada satis hasilati" },
                new() { Date = "22.03.2026", Type = "Gider", Category = "Komisyon", AmountFormatted = "-988,80 TL", Amount = -988.80m, Platform = "Trendyol", Description = "Platform komisyonu" },
                new() { Date = "22.03.2026", Type = "Gider", Category = "Genel Gider", AmountFormatted = "-6.500,00 TL", Amount = -6500m, Platform = "-", Description = "Ofis kirasi" },
                new() { Date = "21.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+1.940,00 TL", Amount = 1940m, Platform = "N11", Description = "N11 satis hasilati" },
                new() { Date = "21.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+4.520,00 TL", Amount = 4520m, Platform = "Amazon", Description = "Amazon satis hasilati" },
                new() { Date = "20.03.2026", Type = "Gider", Category = "Kargo", AmountFormatted = "-380,00 TL", Amount = -380m, Platform = "Yurtici Kargo", Description = "Kargo gideri" },
                new() { Date = "20.03.2026", Type = "Gelir", Category = "Hizmet", AmountFormatted = "+750,00 TL", Amount = 750m, Platform = "-", Description = "Danismanlik geliri" },
                new() { Date = "19.03.2026", Type = "Gider", Category = "Iade", AmountFormatted = "-420,00 TL", Amount = -420m, Platform = "Trendyol", Description = "Urun iade bedeli" },
                new() { Date = "19.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+2.350,00 TL", Amount = 2350m, Platform = "Ciceksepeti", Description = "Ciceksepeti satis hasilati" },
                new() { Date = "18.03.2026", Type = "Gider", Category = "Genel Gider", AmountFormatted = "-1.200,00 TL", Amount = -1200m, Platform = "-", Description = "Elektrik faturasi" },
            ];

            CurrentPage = 1;
            ApplyFilters();
        }, "Gelir/Gider listesi yuklenemedi");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedTypeFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Description.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.Platform.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedTypeFilter != "Tumu")
            filtered = filtered.Where(x => x.Type == SelectedTypeFilter);

        if (SelectedCategory != "Tumu")
            filtered = filtered.Where(x => x.Category == SelectedCategory);

        var filteredList = filtered.ToList();
        TotalCount = filteredList.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        var paged = filteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        Items.Clear();
        foreach (var item in paged)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddEntry()
    {
        // Will open IncomeExpenseEntryDialog
    }

    [RelayCommand]
    private void ExportExcel()
    {
        // Will export filtered data to Excel
        System.Diagnostics.Debug.WriteLine("[IncomeExpenseList] Excel export requested");
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            ApplyFilters();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            ApplyFilters();
        }
    }
}

public class IncomeExpenseListItemDto
{
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
