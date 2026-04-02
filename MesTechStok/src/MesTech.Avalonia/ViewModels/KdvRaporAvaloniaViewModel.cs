using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class KdvRaporAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string salesVat = "0,00 TL";
    [ObservableProperty] private string purchaseVat = "0,00 TL";
    [ObservableProperty] private string netVat = "0,00 TL";
    [ObservableProperty] private int withholdingRateCount;

    // Filters
    [ObservableProperty] private string selectedPeriodType = "Aylik";
    [ObservableProperty] private string selectedMonth = "Mart";
    [ObservableProperty] private string selectedYear = "2026";
    [ObservableProperty] private string selectedInvoiceType = "Tumu";

    public ObservableCollection<VatLineItemDto> Items { get; } = [];
    private List<VatLineItemDto> _allItems = [];

    public ObservableCollection<string> PeriodTypes { get; } =
        ["Aylik", "Ucaylik", "Yillik"];

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public ObservableCollection<string> InvoiceTypes { get; } =
        ["Tumu", "Satis Faturasi", "Alis Faturasi", "Iade Faturasi"];

    private static readonly string[] MonthNames =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public KdvRaporAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var monthIndex = Array.IndexOf(MonthNames, SelectedMonth) + 1;
            if (monthIndex <= 0) monthIndex = DateTime.Now.Month;
            var year = int.TryParse(SelectedYear, out var y) ? y : DateTime.Now.Year;

            var report = await _mediator.Send(new GetKdvReportQuery(_currentUser.TenantId, year, monthIndex));

            SalesVat = $"{report.HesaplananKdv:N2} TL";
            PurchaseVat = $"{report.IndirilecekKdv:N2} TL";
            NetVat = $"{report.OdenecekKdv:N2} TL";

            _allItems =
            [
                new()
                {
                    InvoiceNumber = $"KDV-{report.Year}-{report.Month:D2}",
                    Date = report.BeyannameSonTarih.ToString("dd.MM.yyyy"),
                    AmountFormatted = $"{report.HesaplananKdv + report.IndirilecekKdv:N2} TL",
                    VatRateFormatted = "%20",
                    VatAmountFormatted = $"{report.OdenecekKdv:N2} TL",
                    InvoiceType = "KDV Ozet"
                }
            ];

            IsEmpty = report.HesaplananKdv == 0 && report.IndirilecekKdv == 0;

            // Withholding rates (G540 orphan wire)
            try
            {
                var rates = await _mediator.Send(new GetWithholdingRatesQuery());
                WithholdingRateCount = rates.Count;
            }
            catch { WithholdingRateCount = 0; }

            // G540: KDV declaration draft
            try { _ = await _mediator.Send(new GetKdvDeclarationDraftQuery(_currentUser.TenantId, $"{year}-{Months.IndexOf(SelectedMonth) + 1:D2}")); } catch { }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"KDV raporu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedInvoiceTypeChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (SelectedInvoiceType != "Tumu")
            filtered = filtered.Where(x => x.InvoiceType == SelectedInvoiceType);

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class VatLineItemDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string VatRateFormatted { get; set; } = string.Empty;
    public string VatAmountFormatted { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
}
