using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxSummary;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class VergiTakvimiAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private int overdueCount;
    [ObservableProperty] private int upcomingCount;
    [ObservableProperty] private int completedCount;
    [ObservableProperty] private string taxSummaryText = "—";

    // Filters
    [ObservableProperty] private string selectedMonth = "Mart";
    [ObservableProperty] private string selectedYear = "2026";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string selectedStatusFilter = "Tümü";

    public ObservableCollection<TaxCalendarItemDto> Items { get; } = [];
    private List<TaxCalendarItemDto> _allItems = [];

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public ObservableCollection<string> StatusFilters { get; } =
        ["Tumu", "Gecikmis", "Yaklasan", "Tamamlanan"];

    public VergiTakvimiAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var year = int.TryParse(SelectedYear, out var y) ? y : DateTime.Now.Year;
            var records = await _mediator.Send(
                new GetTaxRecordsQuery(tenantId, Year: year), ct);

            var now = DateTime.Now;
            _allItems = records.Select(r =>
            {
                var daysLeft = (r.DueDate - now).Days;
                var statusColor = r.IsPaid ? "#059669" : daysLeft < 7 ? "#DC2626" : daysLeft < 30 ? "#D97706" : "#059669";
                var statusText = r.IsPaid ? "Odendi" : daysLeft < 0 ? $"{-daysLeft} gun gecikti" : $"{daysLeft} gun kaldi";
                return new TaxCalendarItemDto
                {
                    TaxName = $"{r.TaxType} ({r.Period})",
                    DueDateFormatted = r.DueDate.ToString("dd MMMM yyyy"),
                    StatusText = statusText,
                    AmountFormatted = r.TaxAmount > 0 ? $"{r.TaxAmount:N2} TL" : "—",
                    StatusColor = statusColor
                };
            }).ToList();

            OverdueCount = _allItems.Count(x => x.StatusColor == "#DC2626");
            UpcomingCount = _allItems.Count(x => x.StatusColor == "#D97706");
            CompletedCount = _allItems.Count(x => x.StatusColor == "#059669");

            // Tax summary KPI (G540 orphan wire)
            try
            {
                var period = $"{year}-{Months.IndexOf(SelectedMonth) + 1:D2}";
                var summary = await _mediator.Send(new GetTaxSummaryQuery(tenantId, period), ct);
                TaxSummaryText = $"Toplam: {summary.TotalTax:N2} TL | Odenmis: {summary.TotalPaid:N2} TL";
            }
            catch (Exception ex) { TaxSummaryText = "—"; System.Diagnostics.Debug.WriteLine($"[WARNING] TaxSummary query failed: {ex.Message}"); }

            ApplyFilters();
        }, "Vergi takvimi yuklenirken hata");
    }

    partial void OnSelectedStatusChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (SelectedStatus == "Gecikmis")
            filtered = filtered.Where(x => x.StatusColor == "#DC2626");
        else if (SelectedStatus == "Yaklasan")
            filtered = filtered.Where(x => x.StatusColor == "#D97706");
        else if (SelectedStatus == "Tamamlanan")
            filtered = filtered.Where(x => x.StatusColor == "#059669");

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class TaxCalendarItemDto
{
    public string TaxName { get; set; } = string.Empty;
    public string DueDateFormatted { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#6B7280";
}
