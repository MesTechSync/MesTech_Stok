using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class SabitGiderlerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string monthlyTotal = "0,00 TL";
    [ObservableProperty] private string yearlyTotal = "0,00 TL";
    [ObservableProperty] private int activeCount;

    // Filters
    [ObservableProperty] private string selectedCategory = "Tumu";
    [ObservableProperty] private string selectedPeriod = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<RecurringExpenseItemDto> Items { get; } = [];
    private List<RecurringExpenseItemDto> _allItems = [];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Kira", "Personel", "Teknoloji", "Abonelik", "Sigorta", "Diger"];

    public ObservableCollection<string> Periods { get; } =
        ["Tumu", "Aylik", "Yillik", "Ucaylik"];

    public SabitGiderlerAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var expenses = await _mediator.Send(new GetFixedExpensesQuery(_currentUser.TenantId, true), ct);

            _allItems = expenses.Select(e => new RecurringExpenseItemDto
            {
                Name = e.Name,
                Category = e.Notes ?? "Diger",
                AmountFormatted = $"{e.MonthlyAmount:N2} {e.Currency}",
                Period = e.EndDate.HasValue ? "Yillik" : "Aylik",
                NextPaymentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, e.DayOfMonth > 28 ? 28 : e.DayOfMonth).ToString("dd.MM.yyyy"),
                Amount = e.MonthlyAmount
            }).ToList();

            var monthlyItems = _allItems.Where(x => x.Period == "Aylik").Sum(x => x.Amount);
            var yearlyItems = _allItems.Where(x => x.Period == "Yillik").Sum(x => x.Amount);
            MonthlyTotal = $"{monthlyItems:N2} TL";
            YearlyTotal = $"{monthlyItems * 12 + yearlyItems:N2} TL";
            ActiveCount = _allItems.Count;

            IsEmpty = _allItems.Count == 0;
            ApplyFilters();
        }, "Sabit giderler yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();
    partial void OnSelectedPeriodChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCategory != "Tumu")
            filtered = filtered.Where(x => x.Category == SelectedCategory);

        if (SelectedPeriod != "Tumu")
            filtered = filtered.Where(x => x.Period == SelectedPeriod);

        // Sort
        var sorted = SortColumn switch
        {
            "Name"     => SortAscending ? filtered.OrderBy(x => x.Name).ToList()     : filtered.OrderByDescending(x => x.Name).ToList(),
            "Category" => SortAscending ? filtered.OrderBy(x => x.Category).ToList() : filtered.OrderByDescending(x => x.Category).ToList(),
            "Amount"   => SortAscending ? filtered.OrderBy(x => x.Amount).ToList()   : filtered.OrderByDescending(x => x.Amount).ToList(),
            "Period"   => SortAscending ? filtered.OrderBy(x => x.Period).ToList()   : filtered.OrderByDescending(x => x.Period).ToList(),
            _          => SortAscending ? filtered.OrderBy(x => x.Name).ToList()     : filtered.OrderByDescending(x => x.Name).ToList(),
        };

        Items.Clear();
        foreach (var item in sorted)
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
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "fixed-expenses", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Sabit giderler disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddRecurringExpense()
    {
        // Will open add recurring expense dialog
    }
}

public class RecurringExpenseItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string NextPaymentDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
