using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Gelir/Gider Liste ViewModel — IE-02.
/// Filtreleme (Tur, Kategori, Tarih, Arama) + Sayfalama + Excel Export.
/// </summary>
public partial class IncomeExpenseListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private static readonly CultureInfo TrCulture = new("tr-TR");

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

    public IncomeExpenseListViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Gelir / Gider Listesi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var typeFilter = SelectedTypeFilter == "Tumu" ? null : SelectedTypeFilter;
            var from = DateFrom.HasValue ? DateFrom.Value.UtcDateTime : (DateTime?)null;
            var to = DateTo.HasValue ? DateTo.Value.UtcDateTime : (DateTime?)null;

            var result = await _mediator.Send(
                new GetIncomeExpenseListQuery(
                    _currentUser.TenantId,
                    Type: typeFilter,
                    From: from,
                    To: to,
                    Page: CurrentPage,
                    PageSize: PageSize),
                ct);

            _allItems = result.Items.Select(dto => new IncomeExpenseListItemDto
            {
                Date = dto.Date.ToString("dd.MM.yyyy", TrCulture),
                Type = dto.Type,
                Category = string.Empty,           // not in IncomeExpenseItemDto
                Amount = dto.Amount,
                AmountFormatted = dto.Amount >= 0
                    ? $"+{dto.Amount:N2} TL"
                    : $"{dto.Amount:N2} TL",
                Platform = dto.Source,
                Description = dto.Description
            }).ToList();

            TotalCount = result.TotalCount;
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
