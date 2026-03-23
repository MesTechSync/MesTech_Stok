using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;

namespace MesTech.Avalonia.ViewModels;

public partial class FulfillmentDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private int _fbaStockTotal;
    [ObservableProperty] private int _hlStockTotal;
    [ObservableProperty] private int _activeShipments;
    [ObservableProperty] private int _transitShipments;
    [ObservableProperty] private int _problemShipments;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private ObservableCollection<FulfillmentStock> _inventoryItems = new();
    [ObservableProperty] private ObservableCollection<FulfillmentOrderResult> _recentOrders = new();

    public FulfillmentDashboardViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Fulfillment Yonetimi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var fba = await _mediator.Send(new GetFulfillmentInventoryQuery(
                FulfillmentCenter.AmazonFBA, Array.Empty<string>()), ct);
            var hl = await _mediator.Send(new GetFulfillmentInventoryQuery(
                FulfillmentCenter.Hepsilojistik, Array.Empty<string>()), ct);

            FbaStockTotal = fba.Stocks.Sum(s => s.AvailableQuantity);
            HlStockTotal = hl.Stocks.Sum(s => s.AvailableQuantity);

            var allStocks = fba.Stocks.Concat(hl.Stocks).ToList();
            InventoryItems = new ObservableCollection<FulfillmentStock>(allStocks);

            var orders = await _mediator.Send(new GetFulfillmentOrdersQuery(
                FulfillmentCenter.AmazonFBA, DateTime.UtcNow.AddDays(-30)), ct);
            var hlOrders = await _mediator.Send(new GetFulfillmentOrdersQuery(
                FulfillmentCenter.Hepsilojistik, DateTime.UtcNow.AddDays(-30)), ct);

            var allOrders = orders.Concat(hlOrders).ToList();
            RecentOrders = new ObservableCollection<FulfillmentOrderResult>(allOrders);

            ActiveShipments = allOrders.Count(o => o.Status == "SHIPPED" || o.Status == "PROCESSING");
            TransitShipments = allOrders.Count(o => o.Status == "IN_TRANSIT");
            ProblemShipments = allOrders.Count(o => o.Status == "ERROR" || o.Status == "CANCELLED");

            IsEmpty = allStocks.Count == 0 && allOrders.Count == 0;
        }, "Fulfillment verileri yuklenirken hata");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
