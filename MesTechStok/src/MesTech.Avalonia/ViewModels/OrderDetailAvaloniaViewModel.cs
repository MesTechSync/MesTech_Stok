using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Siparis Detay ViewModel — siparis bilgileri + kalem listesi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class OrderDetailAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


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

    public OrderDetailAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var orders = await _mediator.Send(new GetOrderListQuery(_currentUser.TenantId, 1));

            if (orders.Count > 0)
            {
                var o = orders[0];
                OrderNumber = o.OrderNumber;
                CustomerName = o.CustomerName ?? string.Empty;
                OrderDate = o.OrderDate.ToString("dd.MM.yyyy");
                TotalAmount = o.TotalAmount;
                Platform = o.SourcePlatform ?? string.Empty;
                OrderStatus = o.Status;
                StatusColor = o.Status switch
                {
                    "Tamamlandi" => "#10B981",
                    "Iptal" => "#EF4444",
                    _ => "#F59E0B"
                };
                TrackingNumber = o.TrackingNumber ?? string.Empty;
            }
            else
            {
                IsEmpty = true;
            }

            // TODO: OrderItems — line items query does not exist yet, keeping mock
            OrderItems.Clear();
            OrderItems.Add(new OrderDetailItemDto { ProductName = "Samsung Galaxy S24 Ultra", Sku = "SKU-1001", Quantity = 1, UnitPrice = 54999.99m, LineTotal = 54999.99m });
            OrderItems.Add(new OrderDetailItemDto { ProductName = "Samsung Kilif", Sku = "SKU-2001", Quantity = 2, UnitPrice = 299.90m, LineTotal = 599.80m });
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
