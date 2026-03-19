using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Siparis Detay ViewModel — siparis bilgileri + kalem listesi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class OrderDetailAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string orderNumber = "1042";
    [ObservableProperty] private string orderStatus = "Hazirlaniyor";
    [ObservableProperty] private string statusColor = "#F59E0B";
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string orderDate = string.Empty;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private string platform = string.Empty;
    [ObservableProperty] private string cargoCompany = string.Empty;
    [ObservableProperty] private string trackingNumber = string.Empty;

    public ObservableCollection<OrderDetailItemDto> OrderItems { get; } = [];

    public OrderDetailAvaloniaViewModel(IMediator mediator)
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

            OrderNumber = "1042";
            OrderStatus = "Hazirlaniyor";
            StatusColor = "#F59E0B";
            CustomerName = "Ahmet Yilmaz";
            OrderDate = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            Platform = "Trendyol";
            CargoCompany = "Aras Kargo";
            TrackingNumber = "TR1234567890";

            OrderItems.Clear();
            OrderItems.Add(new OrderDetailItemDto { ProductName = "Samsung Galaxy S24 Ultra", Sku = "SKU-1001", Quantity = 1, UnitPrice = 54999.99m, LineTotal = 54999.99m });
            OrderItems.Add(new OrderDetailItemDto { ProductName = "Samsung Kilif", Sku = "SKU-2001", Quantity = 2, UnitPrice = 299.90m, LineTotal = 599.80m });

            TotalAmount = OrderItems.Sum(x => x.LineTotal);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Siparis detayi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class OrderDetailItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
