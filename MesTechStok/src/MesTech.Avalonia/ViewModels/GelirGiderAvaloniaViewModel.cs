using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class GelirGiderAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    // KPI
    [ObservableProperty] private string totalIncome = "0,00 TL";
    [ObservableProperty] private string totalExpense = "0,00 TL";
    [ObservableProperty] private string netBalance = "0,00 TL";
    [ObservableProperty] private int totalCount;

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedCategory = "Tumu";
    [ObservableProperty] private string selectedTypeFilter = "Tumu";
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;

    public ObservableCollection<GelirGiderItemDto> Items { get; } = [];
    private List<GelirGiderItemDto> _allItems = [];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Satis", "Kargo", "Genel Gider", "Pazaryeri Komisyon", "Iade", "Diger"];

    public ObservableCollection<string> TypeFilters { get; } =
        ["Tumu", "Gelir", "Gider"];

    public GelirGiderAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {

            var tenantId = _tenantProvider.GetCurrentTenantId();
            var typeFilter = SelectedTypeFilter == "Tumu" ? null : SelectedTypeFilter;
            var result = await _mediator.Send(
                new GetIncomeExpenseListQuery(tenantId, Type: typeFilter, PageSize: 100), CancellationToken);

            _allItems = result.Items.Select(i => new GelirGiderItemDto
            {
                Date = i.Date.ToString("dd.MM.yyyy"),
                Description = i.Description,
                Category = i.Source,
                Type = i.Amount >= 0 ? "Gelir" : "Gider",
                Amount = i.Amount,
                AmountFormatted = i.Amount >= 0 ? $"+{i.Amount:N2} TL" : $"{i.Amount:N2} TL"
            }).ToList();

            ApplyFilters();

            var income = _allItems.Where(i => i.Amount > 0).Sum(i => i.Amount);
            var expense = _allItems.Where(i => i.Amount < 0).Sum(i => Math.Abs(i.Amount));
            TotalIncome = $"{income:N2} TL";
            TotalExpense = $"{expense:N2} TL";
            NetBalance = $"{income - expense:N2} TL";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Gelir/Gider verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();
    partial void OnSelectedTypeFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.Description.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCategory != "Tumu")
            filtered = filtered.Where(x => x.Category == SelectedCategory);

        if (SelectedTypeFilter != "Tumu")
            filtered = filtered.Where(x => x.Type == SelectedTypeFilter);

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddEntry()
    {
        // Will open add entry dialog
    }
}

public class GelirGiderItemDto
{
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
