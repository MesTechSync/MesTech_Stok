using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// 360° Müşteri Görünümü — S3-DEV2-03 (Menü 48).
/// Tek müşterinin tüm bilgileri: profil, siparişler, sadakat puanı.
/// INavigationAware ile CustomerId alır.
/// </summary>
public partial class Customer360AvaloniaViewModel : ViewModelBase, INavigationAware
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private Guid _customerId;

    // Profile
    [ObservableProperty] private string customerName = "-";
    [ObservableProperty] private string customerEmail = "-";
    [ObservableProperty] private string customerPhone = "-";

    // Loyalty KPI
    [ObservableProperty] private int loyaltyBalance;
    [ObservableProperty] private int totalEarned;
    [ObservableProperty] private int totalRedeemed;

    // Orders
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private decimal totalSpent;
    public ObservableCollection<OrderSummaryItemDto> RecentOrders { get; } = [];

    // Loyalty History
    public ObservableCollection<LoyaltyTransactionDto> LoyaltyHistory { get; } = [];

    public Customer360AvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task OnNavigatedToAsync(IDictionary<string, object?> parameters)
    {
        if (parameters.TryGetValue("CustomerId", out var idObj) && idObj is Guid id)
            _customerId = id;
        if (parameters.TryGetValue("CustomerName", out var nameObj) && nameObj is string name)
            CustomerName = name;
        return LoadAsync();
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            if (_customerId == Guid.Empty) { IsEmpty = true; return; }

            // Loyalty points
            var points = await _mediator.Send(
                new GetCustomerPointsQuery(_currentUser.TenantId, _customerId), ct);
            LoyaltyBalance = points.AvailableBalance;
            TotalEarned = points.TotalEarned;
            TotalRedeemed = points.TotalRedeemed;

            LoyaltyHistory.Clear();
            foreach (var tx in points.TransactionHistory.Take(10))
                LoyaltyHistory.Add(tx);

            // Recent orders
            var orders = await _mediator.Send(
                new GetOrderListQuery(_currentUser.TenantId, 20), ct);
            // TODO: filter by CustomerId when handler supports it
            RecentOrders.Clear();
            foreach (var o in orders.Take(5))
            {
                RecentOrders.Add(new OrderSummaryItemDto
                {
                    OrderNumber = o.OrderNumber,
                    Date = o.OrderDate.ToString("dd.MM.yyyy"),
                    Amount = o.TotalAmount,
                    Status = o.Status
                });
            }
            OrderCount = orders.Count;
            TotalSpent = orders.Sum(o => o.TotalAmount);
            IsEmpty = false;
        }, "Musteri bilgileri yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class OrderSummaryItemDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
