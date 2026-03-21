using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;

namespace MesTech.Avalonia.ViewModels;

public partial class AccountingDashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalExpense = "0,00 TL";
    [ObservableProperty] private string netProfit = "0,00 TL";
    [ObservableProperty] private string balance = "0,00 TL";
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<AccountingTransactionDto> RecentTransactions { get; } = [];

    public AccountingDashboardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var now = DateTime.Now;
            var summary = await _mediator.Send(
                new GetMonthlySummaryQuery(now.Year, now.Month, Guid.Empty));

            var revenue = summary.TotalSales;
            var expense = summary.TotalExpenses + summary.TotalCommissions + summary.TotalShippingCost;
            var profit = revenue - expense;

            TotalRevenue = revenue.ToString("N2", TrCulture) + " TL";
            TotalExpense = expense.ToString("N2", TrCulture) + " TL";
            NetProfit = profit.ToString("N2", TrCulture) + " TL";
            Balance = (revenue - summary.TotalExpenses).ToString("N2", TrCulture) + " TL";
            LastUpdated = now.ToString("HH:mm:ss");

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
