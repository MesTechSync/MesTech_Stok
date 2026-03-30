using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Karlilik Raporu ViewModel — IE-04.
/// Platform bazli gelir/gider/komisyon/net kar/marj + ozet satiri + export (Excel/PDF/CSV).
/// </summary>
public partial class ProfitabilityReportViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

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

    public ObservableCollection<PlatformProfitViewDto> PlatformProfits { get; } = [];

    public ProfitabilityReportViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Karlilik Raporu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var fromDate = DateFrom?.DateTime ?? DateTime.Now.AddMonths(-1);
            var toDate = DateTo?.DateTime ?? DateTime.Now;

            var report = await _mediator.Send(
                new ProfitabilityReportQuery(_currentUser.TenantId, fromDate, toDate), ct);

            PlatformProfits.Clear();

            foreach (var p in report.ByPlatform)
            {
                var expense = p.Cost + p.Shipping + p.Tax;
                PlatformProfits.Add(new PlatformProfitViewDto
                {
                    Platform = p.Platform,
                    Revenue = p.Revenue,
                    Expense = expense,
                    Commission = p.Commission,
                    NetProfit = p.NetProfit,
                    Margin = p.ProfitMargin,
                    RevenueFormatted = $"{p.Revenue:N2} TL",
                    ExpenseFormatted = $"{expense:N2} TL",
                    CommissionFormatted = $"{p.Commission:N2} TL",
                    NetProfitFormatted = $"{p.NetProfit:N2} TL",
                    MarginFormatted = $"%{p.ProfitMargin:N1}"
                });
            }

            TotalIncome = report.TotalRevenue;
            TotalExpense = report.TotalCost + report.TotalShipping + report.TotalTax;
            var totalCommission = report.TotalCommission;
            TotalNetProfit = report.NetProfit;
            var totalMargin = report.ProfitMargin;

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
    }

    [RelayCommand]
    private void ExportPdf()
    {
    }

    [RelayCommand]
    private void ExportCsv()
    {
    }
}

public class PlatformProfitViewDto
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
