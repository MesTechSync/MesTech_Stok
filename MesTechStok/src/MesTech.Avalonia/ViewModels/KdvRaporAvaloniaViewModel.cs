using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class KdvRaporAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI
    [ObservableProperty] private string salesVat = "0,00 TL";
    [ObservableProperty] private string purchaseVat = "0,00 TL";
    [ObservableProperty] private string netVat = "0,00 TL";

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

    public KdvRaporAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(200); // Will be replaced with MediatR query

            var salesVatAmount = 22586m;
            var purchaseVatAmount = 14120m;
            SalesVat = $"{salesVatAmount:N2} TL";
            PurchaseVat = $"{purchaseVatAmount:N2} TL";
            NetVat = $"{salesVatAmount - purchaseVatAmount:N2} TL";

            _allItems =
            [
                new() { InvoiceNumber = "MES2026-0342", Date = "19.03.2026", AmountFormatted = "4.520,00 TL", VatRateFormatted = "%20", VatAmountFormatted = "904,00 TL", InvoiceType = "Satis Faturasi" },
                new() { InvoiceNumber = "MES2026-0341", Date = "18.03.2026", AmountFormatted = "2.180,00 TL", VatRateFormatted = "%20", VatAmountFormatted = "436,00 TL", InvoiceType = "Satis Faturasi" },
                new() { InvoiceNumber = "ALF-2026-089", Date = "18.03.2026", AmountFormatted = "6.500,00 TL", VatRateFormatted = "%20", VatAmountFormatted = "1.300,00 TL", InvoiceType = "Alis Faturasi" },
                new() { InvoiceNumber = "MES2026-0340", Date = "17.03.2026", AmountFormatted = "1.240,00 TL", VatRateFormatted = "%20", VatAmountFormatted = "248,00 TL", InvoiceType = "Satis Faturasi" },
                new() { InvoiceNumber = "KRG-2026-512", Date = "17.03.2026", AmountFormatted = "380,00 TL", VatRateFormatted = "%20", VatAmountFormatted = "76,00 TL", InvoiceType = "Alis Faturasi" },
                new() { InvoiceNumber = "IAD-2026-015", Date = "16.03.2026", AmountFormatted = "540,00 TL", VatRateFormatted = "%20", VatAmountFormatted = "108,00 TL", InvoiceType = "Iade Faturasi" },
            ];

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
