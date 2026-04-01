using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Gelir/Gider Dashboard ViewModel — IE-01.
/// 4 KPI (Toplam Gelir, Toplam Gider, Net Kar, Kar Marji) + Son 10 islem.
/// </summary>
public partial class IncomeExpenseDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // KPI
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _netProfit;
    [ObservableProperty] private decimal _profitMargin;
    [ObservableProperty] private string _totalIncomeFormatted = "0,00 TL";
    [ObservableProperty] private string _totalExpenseFormatted = "0,00 TL";
    [ObservableProperty] private string _netProfitFormatted = "0,00 TL";
    [ObservableProperty] private string _profitMarginFormatted = "%0,0";

    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<IncomeExpenseTransactionDto> RecentTransactions { get; } = [];

    public IncomeExpenseDashboardViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Gelir / Gider Ozeti";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var summaryTask = _mediator.Send(new GetIncomeExpenseSummaryQuery(_currentUser.TenantId), ct);
            var listTask = _mediator.Send(new GetIncomeExpenseListQuery(_currentUser.TenantId, PageSize: 10), ct);

            await Task.WhenAll(summaryTask, listTask);

            var summary = await summaryTask;
            var list = await listTask;

            RecentTransactions.Clear();

            foreach (var item in list.Items)
            {
                var isIncome = item.Amount > 0;
                RecentTransactions.Add(new IncomeExpenseTransactionDto
                {
                    Date = item.Date.ToString("dd.MM.yyyy"),
                    Type = isIncome ? "Gelir" : "Gider",
                    Category = item.Type,
                    Amount = item.Amount,
                    AmountFormatted = isIncome ? $"+{item.Amount:N2} TL" : $"{item.Amount:N2} TL",
                    Platform = item.Source,
                    Description = item.Description
                });
            }

            TotalIncome = summary.TotalIncome;
            TotalExpense = summary.TotalExpense;
            NetProfit = summary.NetProfit;
            ProfitMargin = TotalIncome > 0 ? NetProfit / TotalIncome * 100 : 0;

            TotalIncomeFormatted = $"{TotalIncome:N2} TL";
            TotalExpenseFormatted = $"{TotalExpense:N2} TL";
            NetProfitFormatted = $"{NetProfit:N2} TL";
            ProfitMarginFormatted = $"%{ProfitMargin:N1}";

            IsEmpty = RecentTransactions.Count == 0;
        }, "Gelir/Gider ozeti yuklenemedi");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddEntry()
    {
        // Will open IncomeExpenseEntryDialog
    }
}

public class IncomeExpenseTransactionDto
{
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
