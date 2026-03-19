using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura listesi ViewModel — filtreleme, arama, sayfalama.
/// DataGrid: InvoiceNumber, RecipientName, Type (badge), Status (badge), Amount, Platform, Date.
/// </summary>
public partial class InvoiceListAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;

    public ObservableCollection<InvoiceListItemDto> Invoices { get; } = [];

    public ObservableCollection<string> InvoiceTypes { get; } =
    [
        "Tumu", "e-Fatura", "e-Arsiv", "e-Ihracat"
    ];

    public ObservableCollection<string> StatusList { get; } =
    [
        "Tumu", "Taslak", "Gonderildi", "Onayli", "Reddedildi"
    ];

    public ObservableCollection<string> PlatformList { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"
    ];

    private List<InvoiceListItemDto> _allInvoices = [];
    private const int PageSize = 20;

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300);

            _allInvoices =
            [
                new() { InvoiceNumber = "MES2026000001", RecipientName = "Yilmaz Elektronik Ltd. Sti.", Type = "e-Fatura", Status = "Onayli", Amount = 24850.00m, Platform = "Trendyol", Date = new DateTime(2026, 3, 17) },
                new() { InvoiceNumber = "MES2026000002", RecipientName = "Demir Bilisim A.S.", Type = "e-Fatura", Status = "Gonderildi", Amount = 18320.50m, Platform = "Hepsiburada", Date = new DateTime(2026, 3, 16) },
                new() { InvoiceNumber = "MES2026000003", RecipientName = "Kaya Ticaret ve Sanayi", Type = "e-Arsiv", Status = "Onayli", Amount = 9750.00m, Platform = "N11", Date = new DateTime(2026, 3, 15) },
                new() { InvoiceNumber = "MES2026000004", RecipientName = "Arslan Mobilya", Type = "e-Fatura", Status = "Reddedildi", Amount = 35400.75m, Platform = "Trendyol", Date = new DateTime(2026, 3, 14) },
                new() { InvoiceNumber = "MES2026000005", RecipientName = "Celik Otomotiv San.", Type = "e-Ihracat", Status = "Taslak", Amount = 67200.00m, Platform = "Amazon", Date = new DateTime(2026, 3, 13) },
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
                i.RecipientName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.InvoiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
            filtered = filtered.Where(i => i.Type == SelectedType);

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(i => i.Status == SelectedStatus);

        if (SelectedPlatform != "Tumu")
            filtered = filtered.Where(i => i.Platform == SelectedPlatform);

        var all = filtered.ToList();
        TotalCount = all.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = 1;

        var paged = all.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        Invoices.Clear();
        foreach (var item in paged)
            Invoices.Add(item);

        IsEmpty = Invoices.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            ApplyFilters();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            ApplyFilters();
        }
    }

    partial void OnSearchTextChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
    partial void OnSelectedTypeChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
    partial void OnSelectedStatusChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
    partial void OnSelectedPlatformChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
}

public class InvoiceListItemDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
