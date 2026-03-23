using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Karlilik Raporu ViewModel — IE-04.
/// Platform bazli gelir/gider/komisyon/net kar/marj + ozet satiri + export (Excel/PDF/CSV).
/// </summary>
public partial class ProfitabilityReportViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Date range
    [ObservableProperty] private DateTimeOffset? _dateFrom;
    [ObservableProperty] private DateTimeOffset? _dateTo;

    // Totals
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _totalNetProfit;

    [ObservableProperty] private string _totalIncomeFormatted = "0,00 TL";
    [ObservableProperty] private string _totalExpenseFormatted = "0,00 TL";
    [ObservableProperty] private string _totalCommissionFormatted = "0,00 TL";
    [ObservableProperty] private string _totalNetProfitFormatted = "0,00 TL";
    [ObservableProperty] private string _totalMarginFormatted = "%0,0";

    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<PlatformProfitDto> PlatformProfits { get; } = [];

    public ProfitabilityReportViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Karlilik Raporu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            await Task.Delay(200, ct); // Will be replaced with MediatR query

            PlatformProfits.Clear();

            var items = new List<PlatformProfitDto>
            {
                new() { Platform = "Trendyol", Revenue = 45200m, Expense = 12500m, Commission = 6780m, NetProfit = 25920m },
                new() { Platform = "Hepsiburada", Revenue = 28400m, Expense = 8200m, Commission = 3550m, NetProfit = 16650m },
                new() { Platform = "N11", Revenue = 12800m, Expense = 4100m, Commission = 1792m, NetProfit = 6908m },
                new() { Platform = "Ciceksepeti", Revenue = 8600m, Expense = 2900m, Commission = 1548m, NetProfit = 4152m },
                new() { Platform = "Amazon", Revenue = 18900m, Expense = 7200m, Commission = 2835m, NetProfit = 8865m },
                new() { Platform = "eBay", Revenue = 6200m, Expense = 2400m, Commission = 806m, NetProfit = 2994m },
                new() { Platform = "Pazarama", Revenue = 3400m, Expense = 1200m, Commission = 340m, NetProfit = 1860m },
            };

            foreach (var item in items)
            {
                item.Margin = item.Revenue > 0 ? item.NetProfit / item.Revenue * 100 : 0;
                item.RevenueFormatted = $"{item.Revenue:N2} TL";
                item.ExpenseFormatted = $"{item.Expense:N2} TL";
                item.CommissionFormatted = $"{item.Commission:N2} TL";
                item.NetProfitFormatted = $"{item.NetProfit:N2} TL";
                item.MarginFormatted = $"%{item.Margin:N1}";
                PlatformProfits.Add(item);
            }

            TotalIncome = items.Sum(i => i.Revenue);
            TotalExpense = items.Sum(i => i.Expense);
            var totalCommission = items.Sum(i => i.Commission);
            TotalNetProfit = items.Sum(i => i.NetProfit);
            var totalMargin = TotalIncome > 0 ? TotalNetProfit / TotalIncome * 100 : 0;

            TotalIncomeFormatted = $"{TotalIncome:N2} TL";
            TotalExpenseFormatted = $"{TotalExpense:N2} TL";
            TotalCommissionFormatted = $"{totalCommission:N2} TL";
            TotalNetProfitFormatted = $"{TotalNetProfit:N2} TL";
            TotalMarginFormatted = $"%{totalMargin:N1}";

            IsEmpty = PlatformProfits.Count == 0;
        }, "Karlilik raporu yuklenemedi");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void ExportExcel()
    {
        System.Diagnostics.Debug.WriteLine("[ProfitabilityReport] Excel export requested");
    }

    [RelayCommand]
    private void ExportPdf()
    {
        System.Diagnostics.Debug.WriteLine("[ProfitabilityReport] PDF export requested");
    }

    [RelayCommand]
    private void ExportCsv()
    {
        System.Diagnostics.Debug.WriteLine("[ProfitabilityReport] CSV export requested");
    }
}

public class PlatformProfitDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expense { get; set; }
    public decimal Commission { get; set; }
    public decimal NetProfit { get; set; }
    public decimal Margin { get; set; }
    public string RevenueFormatted { get; set; } = string.Empty;
    public string ExpenseFormatted { get; set; } = string.Empty;
    public string CommissionFormatted { get; set; } = string.Empty;
    public string NetProfitFormatted { get; set; } = string.Empty;
    public string MarginFormatted { get; set; } = string.Empty;
}
