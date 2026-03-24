using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// R-01: Nakit Akis Raporu ViewModel.
/// Aylik nakit giris/cikis/net/kumulatif hesaplama.
/// </summary>
public partial class CashFlowReportViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private DateTimeOffset? _dateFrom = new DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    [ObservableProperty] private DateTimeOffset? _dateTo = DateTimeOffset.Now;
    [ObservableProperty] private string _totalInflowText = "0.00 TL";
    [ObservableProperty] private string _totalOutflowText = "0.00 TL";
    [ObservableProperty] private string _netFlowText = "0.00 TL";

    public ObservableCollection<CashFlowMonthItem> MonthlyFlows { get; } = [];

    public CashFlowReportViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Nakit Akis Raporu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            MonthlyFlows.Clear();

            // TODO: Replace with IMediator query — await _mediator.Send(new GetCashFlowQuery(...), CancellationToken);
            await Task.Delay(300, CancellationToken);

            var flows = new List<CashFlowMonthItem>
            {
                new("Ocak 2026", 145_000m, 98_500m),
                new("Subat 2026", 132_800m, 105_200m),
                new("Mart 2026", 168_400m, 112_700m),
            };

            decimal cumulative = 0m;
            foreach (var item in flows)
            {
                cumulative += item.Net;
                item.SetCumulative(cumulative);
                MonthlyFlows.Add(item);
            }

            var totalInflow = MonthlyFlows.Sum(f => f.Inflow);
            var totalOutflow = MonthlyFlows.Sum(f => f.Outflow);
            var netFlow = totalInflow - totalOutflow;

            TotalInflowText = $"{totalInflow:N2} TL";
            TotalOutflowText = $"{totalOutflow:N2} TL";
            NetFlowText = $"{netFlow:N2} TL";
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
        System.Diagnostics.Debug.WriteLine("[CashFlowReport] Excel export tetiklendi");
    }

    [RelayCommand]
    private void ExportPdf()
    {
        System.Diagnostics.Debug.WriteLine("[CashFlowReport] PDF export tetiklendi");
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
