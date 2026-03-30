using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// R-01: Nakit Akis Raporu ViewModel — wired to GetCashFlowReportQuery via MediatR.
/// Aylik nakit giris/cikis/net/kumulatif hesaplama.
/// </summary>
public partial class CashFlowReportViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private DateTimeOffset? _dateFrom = new DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    [ObservableProperty] private DateTimeOffset? _dateTo = DateTimeOffset.Now;
    [ObservableProperty] private string _totalInflowText = "0.00 TL";
    [ObservableProperty] private string _totalOutflowText = "0.00 TL";
    [ObservableProperty] private string _netFlowText = "0.00 TL";

    public ObservableCollection<CashFlowMonthItem> MonthlyFlows { get; } = [];

    public CashFlowReportViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Nakit Akis Raporu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            MonthlyFlows.Clear();

            var from = DateFrom?.DateTime ?? new DateTime(DateTime.Now.Year, 1, 1);
            var to = DateTo?.DateTime ?? DateTime.Now;
            var result = await _mediator.Send(
                new GetCashFlowReportQuery(_currentUser.TenantId, from, to), ct);

            // Group entries by month for cumulative view
            var grouped = result.Entries
                .GroupBy(e => new { e.EntryDate.Year, e.EntryDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                    var inflow = g.Where(e => e.Direction == "Inflow").Sum(e => e.Amount);
                    var outflow = g.Where(e => e.Direction == "Outflow").Sum(e => e.Amount);
                    return new CashFlowMonthItem(monthName, inflow, outflow);
                }).ToList();

            decimal cumulative = 0m;
            foreach (var item in grouped)
            {
                cumulative += item.Net;
                item.SetCumulative(cumulative);
                MonthlyFlows.Add(item);
            }

            TotalInflowText = $"{result.TotalInflow:N2} TL";
            TotalOutflowText = $"{result.TotalOutflow:N2} TL";
            NetFlowText = $"{result.NetFlow:N2} TL";
            IsEmpty = MonthlyFlows.Count == 0;
        }, "Nakit akis verisi yukleniyor");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CalculateAsync() => await LoadAsync();

    [RelayCommand]
    private void ExportExcel()
    {
    }

    [RelayCommand]
    private void ExportPdf()
    {
    }
}

public class CashFlowMonthItem
{
    public CashFlowMonthItem(string month, decimal inflow, decimal outflow)
    {
        Month = month;
        Inflow = inflow;
        Outflow = outflow;
    }

    public string Month { get; }
    public decimal Inflow { get; }
    public decimal Outflow { get; }
    public decimal Net => Inflow - Outflow;
    public decimal Cumulative { get; private set; }

    public void SetCumulative(decimal value) => Cumulative = value;

    public string InflowText => Inflow.ToString("N2");
    public string OutflowText => Outflow.ToString("N2");
    public string NetText => Net.ToString("N2");
    public string CumulativeText => Cumulative.ToString("N2");
}
