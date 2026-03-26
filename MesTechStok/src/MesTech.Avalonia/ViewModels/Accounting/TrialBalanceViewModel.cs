using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Mizan Raporu ViewModel — wired to GetTrialBalanceQuery via MediatR.
/// Chain 14 denge kontrolü: Borc = Alacak doğrulaması.
/// </summary>
public partial class TrialBalanceViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private DateTimeOffset? asOfDate = DateTimeOffset.Now;
    [ObservableProperty] private string balanceStatusText = string.Empty;
    [ObservableProperty] private bool isBalanced;
    [ObservableProperty] private string totalDebitSumText = "0.00";
    [ObservableProperty] private string totalCreditSumText = "0.00";
    [ObservableProperty] private string differenceText = "0.00";

    public ObservableCollection<TrialBalanceLineItem> TrialBalanceLines { get; } = [];

    public TrialBalanceViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            TrialBalanceLines.Clear();

            var endDate = AsOfDate?.DateTime ?? DateTime.Now;
            var startDate = new DateTime(endDate.Year, 1, 1);

            var result = await _mediator.Send(
                new GetTrialBalanceQuery(_tenantProvider.GetCurrentTenantId(), startDate, endDate));

            foreach (var line in result.Lines)
            {
                TrialBalanceLines.Add(new(
                    line.AccountCode,
                    line.AccountName,
                    line.ClosingDebit,
                    line.ClosingCredit));
            }

            var totalDebit = result.GrandTotalClosingDebit;
            var totalCredit = result.GrandTotalClosingCredit;
            var diff = totalDebit - totalCredit;

            TotalDebitSumText = totalDebit.ToString("N2");
            TotalCreditSumText = totalCredit.ToString("N2");
            DifferenceText = diff.ToString("N2");
            IsBalanced = Math.Abs(diff) < 0.01m;
            IsEmpty = TrialBalanceLines.Count == 0;
            BalanceStatusText = IsBalanced
                ? $"Mizan dengeli — Borc = Alacak = {totalDebit:N2} TL"
                : $"UYARI: Mizan dengesiz! Fark: {diff:N2} TL";
        }, "Mizan raporu yuklenemedi");
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
