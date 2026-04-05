using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
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

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false; // newest first

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
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var typeFilter = SelectedTypeFilter == "Tumu" ? null : SelectedTypeFilter;
            var result = await _mediator.Send(
                new GetIncomeExpenseListQuery(tenantId, Type: typeFilter, PageSize: 100), ct);

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

            // G540: cash registers
            try { _ = await _mediator.Send(new GetCashRegistersQuery(tenantId), ct); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetCashRegisters failed: {ex.Message}"); }
        }, "Gelir gider verileri yuklenirken hata");
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

        // Sort
        var sortedList = SortColumn switch
        {
            "Date"        => SortAscending ? filtered.OrderBy(x => x.Date).ToList()         : filtered.OrderByDescending(x => x.Date).ToList(),
            "Description" => SortAscending ? filtered.OrderBy(x => x.Description).ToList()  : filtered.OrderByDescending(x => x.Description).ToList(),
            "Category"    => SortAscending ? filtered.OrderBy(x => x.Category).ToList()     : filtered.OrderByDescending(x => x.Category).ToList(),
            "Type"        => SortAscending ? filtered.OrderBy(x => x.Type).ToList()         : filtered.OrderByDescending(x => x.Type).ToList(),
            "Amount"      => SortAscending ? filtered.OrderBy(x => x.Amount).ToList()       : filtered.OrderByDescending(x => x.Amount).ToList(),
            _             => SortAscending ? filtered.OrderBy(x => x.Date).ToList()         : filtered.OrderByDescending(x => x.Date).ToList(),
        };

        Items.Clear();
        foreach (var item in sortedList)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "income-expense", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Gelir gider verileri disa aktarilirken hata");
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
