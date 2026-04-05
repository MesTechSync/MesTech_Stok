using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class NakitAkisAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // KPI
    [ObservableProperty] private string totalInflow = "0,00 TL";
    [ObservableProperty] private string totalOutflow = "0,00 TL";
    [ObservableProperty] private string netCashFlow = "0,00 TL";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string trendSummary = "—";

    // Filters
    [ObservableProperty] private string selectedPeriodType = "Aylik";
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<CashFlowItemDto> Items { get; } = [];
    private List<CashFlowItemDto> _allItems = [];

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false; // newest first

    public ObservableCollection<string> PeriodTypes { get; } =
        ["Gunluk", "Haftalik", "Aylik", "Yillik"];

    public NakitAkisAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var from = StartDate?.DateTime ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = EndDate?.DateTime ?? DateTime.Now;

            var report = await _mediator.Send(new GetCashFlowReportQuery(_currentUser.TenantId, from, to), ct);

            TotalInflow = $"{report.TotalInflow:N2} TL";
            TotalOutflow = $"{report.TotalOutflow:N2} TL";
            NetCashFlow = $"{report.NetFlow:N2} TL";

            _allItems = report.Entries.Select(e => new CashFlowItemDto
            {
                Date = e.EntryDate.ToString("dd.MM.yyyy"),
                Description = e.Description ?? e.CounterpartyName ?? "-",
                InflowFormatted = e.Direction == "Inflow" ? $"+{e.Amount:N2} TL" : "",
                OutflowFormatted = e.Direction == "Outflow" ? $"-{e.Amount:N2} TL" : "",
                BalanceFormatted = "",
                Inflow = e.Direction == "Inflow" ? e.Amount : 0,
                Outflow = e.Direction == "Outflow" ? e.Amount : 0
            }).ToList();

            // Calculate running balance
            var balance = report.TotalInflow - report.TotalOutflow;
            foreach (var item in _allItems)
            {
                item.BalanceFormatted = $"{balance:N2} TL";
                balance -= item.Inflow;
                balance += item.Outflow;
            }

            IsEmpty = _allItems.Count == 0;

            // Cash flow trend (G540 orphan wire)
            try
            {
                var trend = await _mediator.Send(new GetCashFlowTrendQuery(_currentUser.TenantId), ct);
                TrendSummary = $"{trend.Months.Count} ay trend | Kümülatif: {trend.CumulativeNet:N0} TL";
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetCashFlowTrend failed: {ex.Message}"); TrendSummary = "—"; }

            ApplyFilters();
        }, "Nakit akis verileri yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedPeriodTypeChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.Description.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        var sortedList = SortColumn switch
        {
            "Date"        => SortAscending ? filtered.OrderBy(x => x.Date).ToList()        : filtered.OrderByDescending(x => x.Date).ToList(),
            "Description" => SortAscending ? filtered.OrderBy(x => x.Description).ToList() : filtered.OrderByDescending(x => x.Description).ToList(),
            "Inflow"      => SortAscending ? filtered.OrderBy(x => x.Inflow).ToList()      : filtered.OrderByDescending(x => x.Inflow).ToList(),
            "Outflow"     => SortAscending ? filtered.OrderBy(x => x.Outflow).ToList()     : filtered.OrderByDescending(x => x.Outflow).ToList(),
            _             => SortAscending ? filtered.OrderBy(x => x.Date).ToList()        : filtered.OrderByDescending(x => x.Date).ToList(),
        };

        Items.Clear();
        foreach (var item in sortedList)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "cashflow", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Nakit akis verileri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class CashFlowItemDto
{
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InflowFormatted { get; set; } = string.Empty;
    public string OutflowFormatted { get; set; } = string.Empty;
    public string BalanceFormatted { get; set; } = string.Empty;
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
}
