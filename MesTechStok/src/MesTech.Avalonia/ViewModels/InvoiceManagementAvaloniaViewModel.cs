using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// e-Fatura yonetimi ViewModel — Dalga 14/15.
/// DataGrid with No, Tarih, Musteri, Tutar, Durum, Tip columns + Type filter (e-Fatura/e-Arsiv).
/// Will be wired to GetInvoicesPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class InvoiceManagementAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<InvoiceMgmtItemDto> Invoices { get; } = [];

    public ObservableCollection<string> InvoiceTypes { get; } =
    [
        "Tumu", "e-Fatura", "e-Arsiv"
    ];

    private List<InvoiceMgmtItemDto> _allInvoices = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load

            _allInvoices =
            [
                new() { FaturaNo = "MES2026000001", Tarih = new DateTime(2026, 3, 17), Alici = "Yilmaz Elektronik Ltd. Sti.", Tutar = 24850.00m, Durum = "Onayli", Tip = "e-Fatura" },
                new() { FaturaNo = "MES2026000002", Tarih = new DateTime(2026, 3, 16), Alici = "Demir Bilisim A.S.", Tutar = 18320.50m, Durum = "Bekliyor", Tip = "e-Fatura" },
                new() { FaturaNo = "MES2026000003", Tarih = new DateTime(2026, 3, 15), Alici = "Kaya Ticaret ve Sanayi", Tutar = 9750.00m, Durum = "Onayli", Tip = "e-Arsiv" },
                new() { FaturaNo = "MES2026000004", Tarih = new DateTime(2026, 3, 14), Alici = "Arslan Mobilya", Tutar = 35400.75m, Durum = "Reddedildi", Tip = "e-Fatura" },
                new() { FaturaNo = "MES2026000005", Tarih = new DateTime(2026, 3, 13), Alici = "Celik Otomotiv San.", Tutar = 67200.00m, Durum = "Onayli", Tip = "e-Fatura" },
                new() { FaturaNo = "MES2026000006", Tarih = new DateTime(2026, 3, 12), Alici = "Ozturk Gida Paz.", Tutar = 4980.25m, Durum = "Bekliyor", Tip = "e-Arsiv" },
                new() { FaturaNo = "MES2026000007", Tarih = new DateTime(2026, 3, 11), Alici = "Sahin Insaat Malz.", Tutar = 152300.00m, Durum = "Onayli", Tip = "e-Fatura" },
                new() { FaturaNo = "MES2026000008", Tarih = new DateTime(2026, 3, 10), Alici = "Koc Tekstil", Tutar = 28600.50m, Durum = "Bekliyor", Tip = "e-Arsiv" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Faturalar yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Alici.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.FaturaNo.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
        {
            filtered = filtered.Where(i => i.Tip == SelectedType);
        }

        Invoices.Clear();
        foreach (var item in filtered)
            Invoices.Add(item);

        TotalCount = Invoices.Count;
        IsEmpty = Invoices.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateInvoice()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(300); // Simulate creation
            var newInvoice = new InvoiceMgmtItemDto
            {
                FaturaNo = $"MES2026{(_allInvoices.Count + 1):D6}",
                Tarih = DateTime.Now,
                Alici = "Yeni Musteri",
                Tutar = 0.00m,
                Durum = "Bekliyor",
                Tip = "e-Fatura"
            };
            _allInvoices.Insert(0, newInvoice);
            ApplyFilters();
        }
        finally { IsLoading = false; }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allInvoices.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedTypeChanged(string value)
    {
        if (_allInvoices.Count > 0)
            ApplyFilters();
    }
}

public class InvoiceMgmtItemDto
{
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public string Alici { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
}
