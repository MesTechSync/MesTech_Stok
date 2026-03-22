using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Orders management ViewModel — DataGrid with 15 demo orders.
/// Includes Status ComboBox filter + search text.
/// </summary>
public partial class OrdersAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<OrderItemDto> Orders { get; } = [];

    public ObservableCollection<string> Statuses { get; } =
    [
        "Tumu", "Yeni", "Hazirlaniyor", "Kargoda", "Teslim Edildi"
    ];

    private List<OrderItemDto> _allOrders = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load

            _allOrders =
            [
                new() { OrderNo = "SIP-2026-0041", Date = "17.03.2026", Customer = "Ahmet Yilmaz", Amount = "2,450.00 TL", Status = "Yeni" },
                new() { OrderNo = "SIP-2026-0040", Date = "17.03.2026", Customer = "Fatma Demir", Amount = "1,890.50 TL", Status = "Hazirlaniyor" },
                new() { OrderNo = "SIP-2026-0039", Date = "16.03.2026", Customer = "Mehmet Kaya", Amount = "5,200.00 TL", Status = "Kargoda" },
                new() { OrderNo = "SIP-2026-0038", Date = "16.03.2026", Customer = "Ayse Celik", Amount = "890.00 TL", Status = "Teslim Edildi" },
                new() { OrderNo = "SIP-2026-0037", Date = "16.03.2026", Customer = "Ali Ozturk", Amount = "3,150.75 TL", Status = "Yeni" },
                new() { OrderNo = "SIP-2026-0036", Date = "15.03.2026", Customer = "Zeynep Arslan", Amount = "4,720.00 TL", Status = "Hazirlaniyor" },
                new() { OrderNo = "SIP-2026-0035", Date = "15.03.2026", Customer = "Hasan Dogan", Amount = "1,340.25 TL", Status = "Kargoda" },
                new() { OrderNo = "SIP-2026-0034", Date = "15.03.2026", Customer = "Elif Sahin", Amount = "6,890.00 TL", Status = "Teslim Edildi" },
                new() { OrderNo = "SIP-2026-0033", Date = "14.03.2026", Customer = "Burak Yildiz", Amount = "2,100.00 TL", Status = "Teslim Edildi" },
                new() { OrderNo = "SIP-2026-0032", Date = "14.03.2026", Customer = "Selin Korkmaz", Amount = "7,450.50 TL", Status = "Kargoda" },
                new() { OrderNo = "SIP-2026-0031", Date = "14.03.2026", Customer = "Emre Aksoy", Amount = "1,675.00 TL", Status = "Yeni" },
                new() { OrderNo = "SIP-2026-0030", Date = "13.03.2026", Customer = "Deniz Polat", Amount = "3,890.00 TL", Status = "Hazirlaniyor" },
                new() { OrderNo = "SIP-2026-0029", Date = "13.03.2026", Customer = "Gul Erdem", Amount = "12,350.00 TL", Status = "Teslim Edildi" },
                new() { OrderNo = "SIP-2026-0028", Date = "12.03.2026", Customer = "Cem Aydin", Amount = "4,200.75 TL", Status = "Kargoda" },
                new() { OrderNo = "SIP-2026-0027", Date = "12.03.2026", Customer = "Nese Karaca", Amount = "8,900.00 TL", Status = "Yeni" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Siparisler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allOrders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(o =>
                o.OrderNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.Customer.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatus != "Tumu")
        {
            filtered = filtered.Where(o => o.Status == SelectedStatus);
        }

        Orders.Clear();
        foreach (var item in filtered)
            Orders.Add(item);

        TotalCount = Orders.Count;
        IsEmpty = Orders.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allOrders.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        if (_allOrders.Count > 0)
            ApplyFilters();
    }
}

public class OrderItemDto
{
    public string OrderNo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public bool StockDeducted { get; set; }
    public string StockStatusText => StockDeducted ? "Stok Dusuruldu" : "Beklemede";
}
