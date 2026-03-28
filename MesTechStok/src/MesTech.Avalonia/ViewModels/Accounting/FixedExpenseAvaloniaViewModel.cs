using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class FixedExpenseAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string monthlyTotal = "0,00 TL";
    [ObservableProperty] private string yearlyTotal = "0,00 TL";
    [ObservableProperty] private string activeExpenseCount = "0";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedCategory;

    public ObservableCollection<FixedExpenseItemDto> Items { get; } = [];
    private List<FixedExpenseItemDto> _allItems = [];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Kira", "Personel", "Sigorta", "Abonelik", "Vergi", "Diger"];

    public FixedExpenseAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        SelectedCategory = "Tumu";
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetFixedExpensesQuery(_currentUser.TenantId));

            _allItems = result.Select(e => new FixedExpenseItemDto
            {
                Name = e.Name,
                Category = e.SupplierName ?? "Diger",
                MonthlyAmount = e.MonthlyAmount,
                MonthlyFormatted = $"{e.MonthlyAmount:N2} TL",
                Frequency = e.EndDate.HasValue ? "Yillik" : "Aylik",
                NextPayment = e.IsActive
                    ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, e.DayOfMonth > 0 ? e.DayOfMonth : 1)
                        .AddMonths(DateTime.Now.Day >= e.DayOfMonth ? 1 : 0)
                        .ToString("yyyy-MM-dd")
                    : "-",
                IsActive = e.IsActive,
                StatusText = e.IsActive ? "Aktif" : "Pasif"
            }).ToList();

            var active = _allItems.Where(x => x.IsActive).ToList();
            var monthly = active.Where(x => x.Frequency == "Aylik").Sum(x => x.MonthlyAmount);
            var yearly = monthly * 12 + active.Where(x => x.Frequency == "Yillik").Sum(x => x.MonthlyAmount);

            MonthlyTotal = $"{monthly:N2} TL";
            YearlyTotal = $"{yearly:N2} TL";
            ActiveExpenseCount = active.Count.ToString();

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

public class FixedExpenseItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string MonthlyFormatted { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string NextPayment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusText { get; set; } = string.Empty;
}
