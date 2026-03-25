using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;

namespace MesTech.Avalonia.ViewModels;

public partial class ProfitLossAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


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

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var period = _currentPeriod.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
            var result = await _mediator.Send(new GetProfitReportQuery(Guid.Empty, period));

            if (result is not null)
            {
                var expenses = result.TotalCost + result.TotalCommission + result.TotalCargo + result.TotalTax;
                TotalRevenue = $"{result.TotalRevenue:N2} TL";
                TotalExpenses = $"{expenses:N2} TL";
                NetProfit = $"{result.NetProfit:N2} TL";
                ProfitMarginText = $"%{result.ProfitMargin:N1}";

                LineItems.Clear();
                LineItems.Add(new ProfitLossLineItemDto
                {
                    Name = "Total Revenue",
                    Type = "Revenue",
                    AmountFormatted = $"{result.TotalRevenue:N2} TL",
                    PercentFormatted = "%100.0"
                });
                LineItems.Add(new ProfitLossLineItemDto
                {
                    Name = "Cost of Goods Sold",
                    Type = "Expense",
                    AmountFormatted = $"-{result.TotalCost:N2} TL",
                    PercentFormatted = result.TotalRevenue > 0
                        ? $"%{result.TotalCost / result.TotalRevenue * 100:N1}"
                        : "%0.0"
                });
                LineItems.Add(new ProfitLossLineItemDto
                {
                    Name = "Marketplace Commissions",
                    Type = "Expense",
                    AmountFormatted = $"-{result.TotalCommission:N2} TL",
                    PercentFormatted = result.TotalRevenue > 0
                        ? $"%{result.TotalCommission / result.TotalRevenue * 100:N1}"
                        : "%0.0"
                });
                LineItems.Add(new ProfitLossLineItemDto
                {
                    Name = "Shipping Costs",
                    Type = "Expense",
                    AmountFormatted = $"-{result.TotalCargo:N2} TL",
                    PercentFormatted = result.TotalRevenue > 0
                        ? $"%{result.TotalCargo / result.TotalRevenue * 100:N1}"
                        : "%0.0"
                });
                LineItems.Add(new ProfitLossLineItemDto
                {
                    Name = "Tax",
                    Type = "Expense",
                    AmountFormatted = $"-{result.TotalTax:N2} TL",
                    PercentFormatted = result.TotalRevenue > 0
                        ? $"%{result.TotalTax / result.TotalRevenue * 100:N1}"
                        : "%0.0"
                });
            }
            else
            {
                TotalRevenue = "0.00 TL";
                TotalExpenses = "0.00 TL";
                NetProfit = "0.00 TL";
                ProfitMarginText = "%0.0";
                LineItems.Clear();
            }

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
