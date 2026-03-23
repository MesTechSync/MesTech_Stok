using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Gelir/Gider Dashboard ViewModel — IE-01.
/// 4 KPI (Toplam Gelir, Toplam Gider, Net Kar, Kar Marji) + Son 10 islem.
/// </summary>
public partial class IncomeExpenseDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

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

    public IncomeExpenseDashboardViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Gelir / Gider Ozeti";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            await Task.Delay(200, ct); // Will be replaced with MediatR query

            RecentTransactions.Clear();

            var items = new List<IncomeExpenseTransactionDto>
            {
                new() { Date = "24.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+8.240,00 TL", Amount = 8240m, Platform = "Trendyol", Description = "Trendyol satis hasilati" },
                new() { Date = "23.03.2026", Type = "Gider", Category = "Kargo", AmountFormatted = "-620,00 TL", Amount = -620m, Platform = "Aras Kargo", Description = "Kargo gideri" },
                new() { Date = "23.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+3.180,00 TL", Amount = 3180m, Platform = "Hepsiburada", Description = "Hepsiburada satis hasilati" },
                new() { Date = "22.03.2026", Type = "Gider", Category = "Komisyon", AmountFormatted = "-988,80 TL", Amount = -988.80m, Platform = "Trendyol", Description = "Platform komisyonu" },
                new() { Date = "22.03.2026", Type = "Gider", Category = "Genel Gider", AmountFormatted = "-6.500,00 TL", Amount = -6500m, Platform = "-", Description = "Ofis kirasi" },
                new() { Date = "21.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+1.940,00 TL", Amount = 1940m, Platform = "N11", Description = "N11 satis hasilati" },
                new() { Date = "21.03.2026", Type = "Gelir", Category = "Satis", AmountFormatted = "+4.520,00 TL", Amount = 4520m, Platform = "Amazon", Description = "Amazon satis hasilati" },
                new() { Date = "20.03.2026", Type = "Gider", Category = "Kargo", AmountFormatted = "-380,00 TL", Amount = -380m, Platform = "Yurtici Kargo", Description = "Kargo gideri" },
                new() { Date = "20.03.2026", Type = "Gelir", Category = "Hizmet", AmountFormatted = "+750,00 TL", Amount = 750m, Platform = "-", Description = "Danismanlik geliri" },
                new() { Date = "19.03.2026", Type = "Gider", Category = "Iade", AmountFormatted = "-420,00 TL", Amount = -420m, Platform = "Trendyol", Description = "Urun iade bedeli" },
            };

            foreach (var item in items)
                RecentTransactions.Add(item);

            TotalIncome = items.Where(i => i.Amount > 0).Sum(i => i.Amount);
            TotalExpense = items.Where(i => i.Amount < 0).Sum(i => Math.Abs(i.Amount));
            NetProfit = TotalIncome - TotalExpense;
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
