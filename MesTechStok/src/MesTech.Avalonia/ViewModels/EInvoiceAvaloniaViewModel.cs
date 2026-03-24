using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// E-Invoice management ViewModel — DataGrid with No, Tarih, Alici, Tutar, Durum.
/// 8 demo e-invoices + Create button. M1 Avalonia canlandirma — Beta Agent.
/// </summary>
public partial class EInvoiceAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<EInvoiceItemDto> Invoices { get; } = [];

    private List<EInvoiceItemDto> _allInvoices = [];

    public override async Task LoadAsync()
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
                new() { InvoiceNo = "MES2026000001", Date = "17.03.2026", Receiver = "Teknosa A.S.", Amount = 45750.00m, Status = "Onaylandi" },
                new() { InvoiceNo = "MES2026000002", Date = "16.03.2026", Receiver = "MediaMarkt Turkiye", Amount = 128300.00m, Status = "Onaylandi" },
                new() { InvoiceNo = "MES2026000003", Date = "15.03.2026", Receiver = "Vatanbilgisayar Ltd.", Amount = 67890.50m, Status = "Beklemede" },
                new() { InvoiceNo = "MES2026000004", Date = "14.03.2026", Receiver = "Hepsiburada Lojistik", Amount = 23456.00m, Status = "Onaylandi" },
                new() { InvoiceNo = "MES2026000005", Date = "13.03.2026", Receiver = "Trendyol Express", Amount = 89120.75m, Status = "Iptal Edildi" },
                new() { InvoiceNo = "MES2026000006", Date = "12.03.2026", Receiver = "N11 Pazaryeri A.S.", Amount = 34567.00m, Status = "Onaylandi" },
                new() { InvoiceNo = "MES2026000007", Date = "11.03.2026", Receiver = "Ciceksepeti Lojistik", Amount = 15890.25m, Status = "Beklemede" },
                new() { InvoiceNo = "MES2026000008", Date = "10.03.2026", Receiver = "Amazon Turkiye", Amount = 201340.00m, Status = "Onaylandi" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"E-faturalar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.InvoiceNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Receiver.Contains(search, StringComparison.OrdinalIgnoreCase));
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
    private async Task CreateInvoiceAsync()
    {
        // Demo: Add a new invoice to the list
        IsLoading = true;
        try
        {
            await Task.Delay(300); // Simulate creation
            var newInvoice = new EInvoiceItemDto
            {
                InvoiceNo = $"MES2026{_allInvoices.Count + 1:D6}",
                Date = DateTime.Now.ToString("dd.MM.yyyy"),
                Receiver = "Yeni Musteri Ltd.",
                Amount = 0.00m,
                Status = "Taslak"
            };
            _allInvoices.Insert(0, newInvoice);
            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allInvoices.Count > 0)
            ApplyFilters();
    }
}

public class EInvoiceItemDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
