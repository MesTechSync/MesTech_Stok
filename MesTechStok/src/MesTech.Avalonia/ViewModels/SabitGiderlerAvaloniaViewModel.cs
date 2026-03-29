using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
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
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var expenses = await _mediator.Send(new GetFixedExpensesQuery(_currentUser.TenantId, true));

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
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Sabit gider verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
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
