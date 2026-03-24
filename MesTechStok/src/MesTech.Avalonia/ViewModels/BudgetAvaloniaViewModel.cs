using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class BudgetAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // KPI
    [ObservableProperty] private string totalBudget = "0,00 TL";
    [ObservableProperty] private string totalActual = "0,00 TL";
    [ObservableProperty] private string totalRemaining = "0,00 TL";
    [ObservableProperty] private string usageRate = "%0";

    // Filters
    [ObservableProperty] private string selectedMonth = "Mart";
    [ObservableProperty] private string selectedYear = "2026";

    public ObservableCollection<BudgetLineItemDto> Items { get; } = [];

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public BudgetAvaloniaViewModel(IMediator mediator)
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

            var items = new List<BudgetLineItemDto>
            {
                new() { CategoryName = "Kira", BudgetFormatted = "15.000,00 TL", ActualFormatted = "15.000,00 TL", RemainingFormatted = "0,00 TL", StatusText = "Tamamlandi", Budget = 15000, Actual = 15000 },
                new() { CategoryName = "Personel", BudgetFormatted = "40.000,00 TL", ActualFormatted = "38.000,00 TL", RemainingFormatted = "2.000,00 TL", StatusText = "Normal", Budget = 40000, Actual = 38000 },
                new() { CategoryName = "Kargo", BudgetFormatted = "10.000,00 TL", ActualFormatted = "12.000,00 TL", RemainingFormatted = "-2.000,00 TL", StatusText = "ASIM", Budget = 10000, Actual = 12000 },
                new() { CategoryName = "Reklam", BudgetFormatted = "20.000,00 TL", ActualFormatted = "8.000,00 TL", RemainingFormatted = "12.000,00 TL", StatusText = "Normal", Budget = 20000, Actual = 8000 },
                new() { CategoryName = "Teknoloji", BudgetFormatted = "5.000,00 TL", ActualFormatted = "4.200,00 TL", RemainingFormatted = "800,00 TL", StatusText = "Normal", Budget = 5000, Actual = 4200 },
            };

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            var budget = items.Sum(x => x.Budget);
            var actual = items.Sum(x => x.Actual);
            var remaining = budget - actual;
            var rate = budget > 0 ? actual / budget * 100 : 0;

            TotalBudget = $"{budget:N2} TL";
            TotalActual = $"{actual:N2} TL";
            TotalRemaining = $"{remaining:N2} TL";
            UsageRate = $"%{rate:N0}";

            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Butce verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class BudgetLineItemDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string BudgetFormatted { get; set; } = string.Empty;
    public string ActualFormatted { get; set; } = string.Empty;
    public string RemainingFormatted { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Actual { get; set; }
}
