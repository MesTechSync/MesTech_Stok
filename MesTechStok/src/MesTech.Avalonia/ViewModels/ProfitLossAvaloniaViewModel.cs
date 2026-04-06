using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class ProfitLossAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // KPI
    [ObservableProperty] private string totalRevenue = "0.00 TL";
    [ObservableProperty] private string totalExpenses = "0.00 TL";
    [ObservableProperty] private string netProfit = "0.00 TL";
    [ObservableProperty] private string profitMarginText = "%0.0";
    [ObservableProperty] private string periodLabel = string.Empty;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    private DateTime _currentPeriod = DateTime.Now;
    private List<ProfitLossLineItemDto> _allItems = [];

    public ObservableCollection<ProfitLossLineItemDto> LineItems { get; } = [];

    public ProfitLossAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        UpdatePeriodLabel();
    }

    private void UpdatePeriodLabel()
    {
        PeriodLabel = _currentPeriod.ToString("MMMM yyyy",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var period = _currentPeriod.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
            var result = await _mediator.Send(new GetProfitReportQuery(_currentUser.TenantId, period), ct) ?? new();

            if (result is not null)
            {
                var expenses = result.TotalCost + result.TotalCommission + result.TotalCargo + result.TotalTax;
                TotalRevenue = $"{result.TotalRevenue:N2} TL";
                TotalExpenses = $"{expenses:N2} TL";
                NetProfit = $"{result.NetProfit:N2} TL";
                ProfitMarginText = $"%{result.ProfitMargin:N1}";

                _allItems =
                [
                    new ProfitLossLineItemDto
                    {
                        Name = "Total Revenue",
                        Type = "Revenue",
                        AmountFormatted = $"{result.TotalRevenue:N2} TL",
                        PercentFormatted = "%100.0"
                    },
                    new ProfitLossLineItemDto
                    {
                        Name = "Cost of Goods Sold",
                        Type = "Expense",
                        AmountFormatted = $"-{result.TotalCost:N2} TL",
                        PercentFormatted = result.TotalRevenue > 0
                            ? $"%{result.TotalCost / result.TotalRevenue * 100:N1}"
                            : "%0.0"
                    },
                    new ProfitLossLineItemDto
                    {
                        Name = "Marketplace Commissions",
                        Type = "Expense",
                        AmountFormatted = $"-{result.TotalCommission:N2} TL",
                        PercentFormatted = result.TotalRevenue > 0
                            ? $"%{result.TotalCommission / result.TotalRevenue * 100:N1}"
                            : "%0.0"
                    },
                    new ProfitLossLineItemDto
                    {
                        Name = "Shipping Costs",
                        Type = "Expense",
                        AmountFormatted = $"-{result.TotalCargo:N2} TL",
                        PercentFormatted = result.TotalRevenue > 0
                            ? $"%{result.TotalCargo / result.TotalRevenue * 100:N1}"
                            : "%0.0"
                    },
                    new ProfitLossLineItemDto
                    {
                        Name = "Tax",
                        Type = "Expense",
                        AmountFormatted = $"-{result.TotalTax:N2} TL",
                        PercentFormatted = result.TotalRevenue > 0
                            ? $"%{result.TotalTax / result.TotalRevenue * 100:N1}"
                            : "%0.0"
                    }
                ];

                ApplySort();
            }
            else
            {
                TotalRevenue = "0.00 TL";
                TotalExpenses = "0.00 TL";
                NetProfit = "0.00 TL";
                ProfitMarginText = "%0.0";
                _allItems = [];
                LineItems.Clear();
            }

            IsEmpty = LineItems.Count == 0;
        }, "Kar/zarar raporu yuklenirken hata");
    }

    private void ApplySort()
    {
        var sorted = _allItems.AsEnumerable();
        sorted = SortColumn switch
        {
            "Name"            => SortAscending ? sorted.OrderBy(x => x.Name)            : sorted.OrderByDescending(x => x.Name),
            "Type"            => SortAscending ? sorted.OrderBy(x => x.Type)            : sorted.OrderByDescending(x => x.Type),
            "AmountFormatted" => SortAscending ? sorted.OrderBy(x => x.AmountFormatted) : sorted.OrderByDescending(x => x.AmountFormatted),
            "PercentFormatted"=> SortAscending ? sorted.OrderBy(x => x.PercentFormatted): sorted.OrderByDescending(x => x.PercentFormatted),
            _                 => sorted
        };

        LineItems.Clear();
        foreach (var item in sorted)
            LineItems.Add(item);
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplySort();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new ExportReportCommand(_currentUser.TenantId, "profit-loss", "xlsx"), ct);

            if (result?.FileData.Length > 0)
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MesTech_Exports");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, result.FileName);
                await File.WriteAllBytesAsync(path, result.FileData.ToArray(), ct);
            }
        }, "Excel export sirasinda hata");
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
