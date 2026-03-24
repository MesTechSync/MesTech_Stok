using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Mizan Raporu ViewModel — Chain 14 denge kontrolü.
/// DEV 1'in GLAccount + JournalEntry sistemine bağlanacak.
/// </summary>
public partial class TrialBalanceViewModel : ViewModelBase
{
    [ObservableProperty] private DateTimeOffset? asOfDate = DateTimeOffset.Now;
    [ObservableProperty] private string balanceStatusText = string.Empty;
    [ObservableProperty] private bool isBalanced;
    [ObservableProperty] private string totalDebitSumText = "0.00";
    [ObservableProperty] private string totalCreditSumText = "0.00";
    [ObservableProperty] private string differenceText = "0.00";

    public ObservableCollection<TrialBalanceLineItem> TrialBalanceLines { get; } = [];

    public override async Task LoadAsync()
    {
        TrialBalanceLines.Clear();
        await Task.Delay(200);

        TrialBalanceLines.Add(new("100", "Kasa", 45_000m, 38_500m));
        TrialBalanceLines.Add(new("102", "Bankalar", 128_750m, 95_200m));
        TrialBalanceLines.Add(new("120", "Alıcılar", 67_300m, 52_100m));
        TrialBalanceLines.Add(new("153", "Ticari Mallar", 89_400m, 71_600m));
        TrialBalanceLines.Add(new("320", "Satıcılar", 42_800m, 56_300m));
        TrialBalanceLines.Add(new("600", "Yurtiçi Satışlar", 0m, 185_750m));
        TrialBalanceLines.Add(new("621", "SMM", 142_100m, 0m));
        TrialBalanceLines.Add(new("770", "Genel Yönetim Gid.", 15_200m, 0m));

        var totalDebit = TrialBalanceLines.Sum(l => l.TotalDebit);
        var totalCredit = TrialBalanceLines.Sum(l => l.TotalCredit);
        var diff = totalDebit - totalCredit;

        TotalDebitSumText = totalDebit.ToString("N2");
        TotalCreditSumText = totalCredit.ToString("N2");
        DifferenceText = diff.ToString("N2");
        IsBalanced = Math.Abs(diff) < 0.01m;
        IsEmpty = TrialBalanceLines.Count == 0;
        BalanceStatusText = IsBalanced
            ? $"Mizan dengeli — Borc = Alacak = {totalDebit:N2} TL"
            : $"UYARI: Mizan dengesiz! Fark: {diff:N2} TL";
    }

    [RelayCommand]
    private async Task CalculateAsync() => await LoadAsync();

    [RelayCommand]
    private void ExportPdf()
    {
        System.Diagnostics.Debug.WriteLine("[TrialBalance] PDF export tetiklendi");
    }
}

public class TrialBalanceLineItem
{
    public TrialBalanceLineItem(string code, string name, decimal debit, decimal credit)
    {
        AccountCode = code;
        AccountName = name;
        TotalDebit = debit;
        TotalCredit = credit;
    }

    public string AccountCode { get; }
    public string AccountName { get; }
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }
    public decimal Balance => TotalDebit - TotalCredit;

    public string TotalDebitText => TotalDebit.ToString("N2");
    public string TotalCreditText => TotalCredit.ToString("N2");
    public string BalanceText => Balance.ToString("N2");
}
