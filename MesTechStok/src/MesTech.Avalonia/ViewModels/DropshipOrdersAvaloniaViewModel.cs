using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Siparisler ViewModel — siparis DataGrid + durum filtre + tedarikci iletme.
/// </summary>
public partial class DropshipOrdersAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedStatus = "Tumu";

    public ObservableCollection<DropshipOrderItemDto> Orders { get; } = [];
    public ObservableCollection<string> StatusOptions { get; } = ["Tumu", "Yeni", "Tedarikçiye İletildi", "Kargoda", "Teslim Edildi", "İptal"];

    private List<DropshipOrderItemDto> _allOrders = [];

    public DropshipOrdersAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(300);

            _allOrders =
            [
                new() { OrderId = "DS-2026-001", Customer = "Ahmet Yilmaz", Supplier = "ABC Elektronik", Status = "Kargoda", CustomerPrice = 64_999m, SupplierPrice = 52_000m, NetProfit = 12_999m },
                new() { OrderId = "DS-2026-002", Customer = "Zeynep Kaya", Supplier = "XYZ Bilisim", Status = "Yeni", CustomerPrice = 11_499m, SupplierPrice = 8_500m, NetProfit = 2_999m },
                new() { OrderId = "DS-2026-003", Customer = "Murat Demir", Supplier = "ABC Elektronik", Status = "Tedarikçiye İletildi", CustomerPrice = 3_299m, SupplierPrice = 2_400m, NetProfit = 899m },
                new() { OrderId = "DS-2026-004", Customer = "Elif Can", Supplier = "Guney Aksesuar", Status = "Teslim Edildi", CustomerPrice = 18_799m, SupplierPrice = 14_200m, NetProfit = 4_599m },
                new() { OrderId = "DS-2026-005", Customer = "Burak Ozturk", Supplier = "Delta Depo", Status = "İptal", CustomerPrice = 7_499m, SupplierPrice = 5_800m, NetProfit = 0m },
                new() { OrderId = "DS-2026-006", Customer = "Selin Arslan", Supplier = "ABC Elektronik", Status = "Kargoda", CustomerPrice = 28_990m, SupplierPrice = 22_500m, NetProfit = 6_490m },
                new() { OrderId = "DS-2026-007", Customer = "Omer Yildiz", Supplier = "XYZ Bilisim", Status = "Yeni", CustomerPrice = 2_199m, SupplierPrice = 1_600m, NetProfit = 599m },
                new() { OrderId = "DS-2026-008", Customer = "Ayse Celik", Supplier = "Guney Aksesuar", Status = "Teslim Edildi", CustomerPrice = 4_599m, SupplierPrice = 3_200m, NetProfit = 1_399m },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dropshipping siparisleri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allOrders.AsEnumerable();
        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(o => o.Status == SelectedStatus);

        Orders.Clear();
        foreach (var item in filtered)
            Orders.Add(item);

        TotalCount = Orders.Count;
        IsEmpty = Orders.Count == 0;
    }

    [RelayCommand]
    private async Task ForwardToSupplierAsync(DropshipOrderItemDto? order)
    {
        if (order is null || order.Status != "Yeni") return;

        order.Status = "Tedarikçiye İletildi";
        var index = Orders.IndexOf(order);
        if (index >= 0)
        {
            Orders.RemoveAt(index);
            Orders.Insert(index, order);
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSelectedStatusChanged(string value)
    {
        if (_allOrders.Count > 0)
            ApplyFilters();
    }
}

public class DropshipOrderItemDto
{
    public string OrderId { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CustomerPrice { get; set; }
    public decimal SupplierPrice { get; set; }
    public decimal NetProfit { get; set; }
}
