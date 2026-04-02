using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;
using MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Accounting.Queries.GetPendingReviews;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class AccountingDashboardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private static readonly CultureInfo TrCulture = new("tr-TR");


    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalExpense = "0,00 TL";
    [ObservableProperty] private string netProfit = "0,00 TL";
    [ObservableProperty] private string balance = "0,00 TL";
    [ObservableProperty] private string totalAssets = "0,00 TL";
    [ObservableProperty] private string totalLiabilities = "0,00 TL";
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<AccountingTransactionDto> RecentTransactions { get; } = [];

    public AccountingDashboardAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var now = DateTime.Now;
            var summary = await _mediator.Send(
                new GetMonthlySummaryQuery(now.Year, now.Month, _currentUser.TenantId));

            var revenue = summary.TotalSales;
            var expense = summary.TotalExpenses + summary.TotalCommissions + summary.TotalShippingCost;
            var profit = revenue - expense;

            TotalRevenue = revenue.ToString("N2", TrCulture) + " TL";
            TotalExpense = expense.ToString("N2", TrCulture) + " TL";
            NetProfit = profit.ToString("N2", TrCulture) + " TL";
            Balance = (revenue - summary.TotalExpenses).ToString("N2", TrCulture) + " TL";
            LastUpdated = now.ToString("HH:mm:ss");

            // Balance sheet KPIs (G540 orphan wire)
            try
            {
                var bs = await _mediator.Send(new GetBalanceSheetQuery(_currentUser.TenantId, now));
                TotalAssets = bs.Assets.Total.ToString("N2", TrCulture) + " TL";
                TotalLiabilities = bs.Liabilities.Total.ToString("N2", TrCulture) + " TL";
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] Balance sheet query failed: {ex.Message}"); }

            // G540 orphan: additional accounting queries (optional — failures logged, not blocking)
            try { _ = await _mediator.Send(new GetAccountingExpensesQuery(_currentUser.TenantId, now.AddMonths(-1), now)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] AccountingExpenses query failed: {ex.Message}"); }
            try { _ = await _mediator.Send(new GetAccountingPeriodsQuery(_currentUser.TenantId, now.Year)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] AccountingPeriods query failed: {ex.Message}"); }
            try { _ = await _mediator.Send(new GetPendingReviewsQuery(_currentUser.TenantId)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] PendingReviews query failed: {ex.Message}"); }
            try { _ = await _mediator.Send(new GetFifoCOGSQuery(_currentUser.TenantId)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] FifoCOGS query failed: {ex.Message}"); }

            RecentTransactions.Clear();
            foreach (var p in summary.SalesByPlatform)
            {
                RecentTransactions.Add(new AccountingTransactionDto
                {
                    Date = now.ToString("dd.MM.yyyy"),
                    Description = $"{p.Platform} satis hasilati",
                    Category = "Satis",
                    Type = "Gelir",
                    AmountFormatted = $"+{p.Sales.ToString("N2", TrCulture)} TL"
                });
            }

            if (summary.TotalShippingCost > 0)
            {
                RecentTransactions.Add(new AccountingTransactionDto
                {
                    Date = now.ToString("dd.MM.yyyy"),
                    Description = "Toplam kargo gideri",
                    Category = "Kargo",
                    Type = "Gider",
                    AmountFormatted = $"-{summary.TotalShippingCost.ToString("N2", TrCulture)} TL"
                });
            }

            if (summary.TotalCommissions > 0)
            {
                RecentTransactions.Add(new AccountingTransactionDto
                {
                    Date = now.ToString("dd.MM.yyyy"),
                    Description = "Toplam platform komisyonu",
                    Category = "Komisyon",
                    Type = "Gider",
                    AmountFormatted = $"-{summary.TotalCommissions.ToString("N2", TrCulture)} TL"
                });
            }

            IsEmpty = RecentTransactions.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Muhasebe verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class AccountingTransactionDto
{
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
}
