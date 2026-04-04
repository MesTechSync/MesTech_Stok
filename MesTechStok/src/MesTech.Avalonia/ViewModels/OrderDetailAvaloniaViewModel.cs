using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderDetail;
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


    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<OrderDetailItemDto> _allOrderItems = [];

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
        await SafeExecuteAsync(async ct =>
        {
            var orders = await _mediator.Send(new GetOrderListQuery(_currentUser.TenantId, 1), ct);

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

                // GetOrderDetailQuery ile line items cek
                var detail = await _mediator.Send(new GetOrderDetailQuery(_currentUser.TenantId, o.Id), ct);
                _allOrderItems.Clear();
                if (detail?.LineItems is { Count: > 0 })
                {
                    foreach (var li in detail.LineItems)
                    {
                        _allOrderItems.Add(new OrderDetailItemDto
                        {
                            ProductName = li.ProductName,
                            Sku = li.SKU,
                            Quantity = li.Quantity,
                            UnitPrice = li.UnitPrice,
                            LineTotal = li.TotalPrice
                        });
                    }
                }
                CargoCompany = detail?.CargoProvider ?? string.Empty;
            }
            else
            {
                IsEmpty = true;
                _allOrderItems.Clear();
            }

            ApplyFilter();
        }, "Siparis detayi yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        OrderItems.Clear();

        var filtered = _allOrderItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(i =>
                i.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
            OrderItems.Add(item);
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
