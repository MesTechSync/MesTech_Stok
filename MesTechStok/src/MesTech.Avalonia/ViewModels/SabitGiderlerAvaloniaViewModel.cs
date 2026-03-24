using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class SabitGiderlerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

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

    public SabitGiderlerAvaloniaViewModel(IMediator mediator)
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

            _allItems =
            [
                new() { Name = "Ofis Kirasi", Category = "Kira", AmountFormatted = "15.000,00 TL", Period = "Aylik", NextPaymentDate = "01.04.2026", Amount = 15000 },
                new() { Name = "AWS Sunucu", Category = "Teknoloji", AmountFormatted = "2.100,00 TL", Period = "Aylik", NextPaymentDate = "01.04.2026", Amount = 2100 },
                new() { Name = "Muhasebeci", Category = "Personel", AmountFormatted = "5.000,00 TL", Period = "Aylik", NextPaymentDate = "01.04.2026", Amount = 5000 },
                new() { Name = "Internet + Telefon", Category = "Abonelik", AmountFormatted = "1.800,00 TL", Period = "Aylik", NextPaymentDate = "15.04.2026", Amount = 1800 },
                new() { Name = "Isyeri Sigortasi", Category = "Sigorta", AmountFormatted = "4.500,00 TL", Period = "Yillik", NextPaymentDate = "15.06.2026", Amount = 4500 },
                new() { Name = "Domain + SSL", Category = "Teknoloji", AmountFormatted = "750,00 TL", Period = "Yillik", NextPaymentDate = "01.09.2026", Amount = 750 },
            ];

            var monthlyItems = _allItems.Where(x => x.Period == "Aylik").Sum(x => x.Amount);
            var yearlyItems = _allItems.Where(x => x.Period == "Yillik").Sum(x => x.Amount);
            MonthlyTotal = $"{monthlyItems:N2} TL";
            YearlyTotal = $"{monthlyItems * 12 + yearlyItems:N2} TL";
            ActiveCount = _allItems.Count;

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
