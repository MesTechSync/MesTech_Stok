using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class ProfitLossAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI
    [ObservableProperty] private string totalRevenue = "0.00 TL";
    [ObservableProperty] private string totalExpenses = "0.00 TL";
    [ObservableProperty] private string netProfit = "0.00 TL";
    [ObservableProperty] private string profitMarginText = "%0.0";
    [ObservableProperty] private string periodLabel = string.Empty;

    private DateTime _currentPeriod = DateTime.Now;

    public ObservableCollection<ProfitLossLineItemDto> LineItems { get; } = [];

    public ProfitLossAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        UpdatePeriodLabel();
    }

    private void UpdatePeriodLabel()
    {
        PeriodLabel = _currentPeriod.ToString("MMMM yyyy",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            var revenue = 125480m;
            var expenses = 87320m;
            var profit = revenue - expenses;
            var margin = revenue > 0 ? profit / revenue * 100 : 0;

            TotalRevenue = $"{revenue:N2} TL";
            TotalExpenses = $"{expenses:N2} TL";
            NetProfit = $"{profit:N2} TL";
            ProfitMarginText = $"%{margin:N1}";

            LineItems.Clear();
            var items = new List<ProfitLossLineItemDto>
            {
                new() { Name = "Domestic Sales", Type = "Revenue", AmountFormatted = "98,240.00 TL", PercentFormatted = "%78.3" },
                new() { Name = "Marketplace Sales", Type = "Revenue", AmountFormatted = "27,240.00 TL", PercentFormatted = "%21.7" },
                new() { Name = "Cost of Goods Sold", Type = "Expense", AmountFormatted = "-52,400.00 TL", PercentFormatted = "%60.0" },
                new() { Name = "Shipping Costs", Type = "Expense", AmountFormatted = "-12,680.00 TL", PercentFormatted = "%14.5" },
                new() { Name = "Marketplace Commissions", Type = "Expense", AmountFormatted = "-8,740.00 TL", PercentFormatted = "%10.0" },
                new() { Name = "General Admin Expenses", Type = "Expense", AmountFormatted = "-9,200.00 TL", PercentFormatted = "%10.5" },
                new() { Name = "Personnel Expenses", Type = "Expense", AmountFormatted = "-4,300.00 TL", PercentFormatted = "%4.9" },
            };
            foreach (var item in items)
                LineItems.Add(item);

            IsEmpty = LineItems.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load profit/loss report: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task PrevMonth()
    {
        _currentPeriod = _currentPeriod.AddMonths(-1);
        UpdatePeriodLabel();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextMonth()
    {
        _currentPeriod = _currentPeriod.AddMonths(1);
        UpdatePeriodLabel();
        await LoadAsync();
    }
}

public class ProfitLossLineItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string PercentFormatted { get; set; } = string.Empty;
}
