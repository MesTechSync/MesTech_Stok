using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderDetail;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Siparis Detay ViewModel — siparis bilgileri + kalem listesi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class OrderDetailAvaloniaViewModel : ViewModelBase, INavigationAware
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly INavigationService _nav;

    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<OrderDetailItemDto> _allOrderItems = [];
    private Guid _orderId;

    [ObservableProperty] private string orderNumber = "-";
    [ObservableProperty] private string orderStatus = "-";
    [ObservableProperty] private string statusColor = "#64748B";
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string customerAddress = string.Empty;
    [ObservableProperty] private string orderDate = string.Empty;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal taxAmount;
    [ObservableProperty] private string platform = string.Empty;
    [ObservableProperty] private string cargoCompany = string.Empty;
    [ObservableProperty] private string trackingNumber = string.Empty;
    [ObservableProperty] private string paymentStatus = string.Empty;
    [ObservableProperty] private string notes = string.Empty;

    public ObservableCollection<OrderDetailItemDto> OrderItems { get; } = [];

    public OrderDetailAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, INavigationService nav)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _nav = nav;
    }

    public Task OnNavigatedToAsync(IDictionary<string, object?> parameters)
    {
        if (parameters.TryGetValue("OrderId", out var idObj) && idObj is Guid id)
            _orderId = id;
        return LoadAsync();
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            if (_orderId == Guid.Empty)
            {
                // Fallback: ilk siparişi göster (eski davranış)
                var orders = await _mediator.Send(new GetOrderListQuery(_currentUser.TenantId, 1), ct);
                if (orders.Count > 0) _orderId = orders[0].Id;
                else { IsEmpty = true; return; }
            }

            var detail = await _mediator.Send(new GetOrderDetailQuery(_currentUser.TenantId, _orderId), ct);
            if (detail is null) { IsEmpty = true; return; }

            OrderNumber = detail.OrderNumber;
            CustomerName = detail.CustomerName ?? string.Empty;
            CustomerAddress = detail.ShippingAddress ?? string.Empty;
            OrderDate = detail.OrderDate.ToString("dd.MM.yyyy HH:mm");
            TotalAmount = detail.TotalAmount;
            SubTotal = detail.SubTotal;
            TaxAmount = detail.TaxAmount;
            Platform = detail.SourcePlatform?.ToString() ?? string.Empty;
            PaymentStatus = detail.PaymentStatus ?? string.Empty;
            Notes = detail.Notes ?? string.Empty;
            TrackingNumber = detail.TrackingNumber ?? string.Empty;
            CargoCompany = detail.CargoProvider ?? string.Empty;
            OrderStatus = detail.Status.ToString();
            StatusColor = detail.Status.ToString() switch
            {
                "Completed" or "Delivered" => "#10B981",
                "Cancelled" => "#EF4444",
                "Shipped" => "#3B82F6",
                "Processing" => "#F59E0B",
                _ => "#64748B"
            };

            _allOrderItems.Clear();
            foreach (var li in detail.LineItems)
            {
                _allOrderItems.Add(new OrderDetailItemDto
                {
                    ProductName = li.ProductName,
                    Sku = li.SKU,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    LineTotal = li.TotalPrice,
                    TaxAmount = li.TaxAmount
                });
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

    /// <summary>D2-018: Sipariş detayından fatura oluşturma ekranına geçiş.</summary>
    [RelayCommand]
    private async Task CreateInvoice() => await _nav.NavigateToAsync("InvoiceCreate", new Dictionary<string, object?>
    {
        ["OrderId"] = _orderId,
        ["OrderNumber"] = OrderNumber,
        ["CustomerName"] = CustomerName,
        ["TotalAmount"] = TotalAmount,
        ["TaxAmount"] = TaxAmount
    });
}

public class OrderDetailItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxAmount { get; set; }
}
