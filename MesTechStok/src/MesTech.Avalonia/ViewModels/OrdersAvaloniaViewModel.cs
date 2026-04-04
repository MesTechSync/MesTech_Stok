using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Orders management ViewModel — wired to GetOrderListQuery via MediatR.
/// Includes Status ComboBox filter + search text.
/// </summary>
public partial class OrdersAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedStatus = "Tümü";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<OrderItemDto> Orders { get; } = [];

    public ObservableCollection<string> Statuses { get; } =
    [
        "Tümü", "Yeni", "Hazırlanıyor", "Kargoda", "Teslim Edildi"
    ];

    private List<OrderItemDto> _allOrders = [];

    public OrdersAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetOrderListQuery(_tenantProvider.GetCurrentTenantId(), 100), ct);

            _allOrders = result.Select(o => new OrderItemDto
            {
                OrderNo = o.OrderNumber,
                Date = o.OrderDate.ToString("dd.MM.yyyy"),
                Customer = o.CustomerName ?? "-",
                Amount = $"{o.TotalAmount:N2} TL",
                Status = o.Status,
                Platform = o.SourcePlatform ?? "-",
                StockDeducted = o.Status is "Kargoda" or "Teslim Edildi"
            }).ToList();

            ApplyFilters();
        }, "Siparisler yuklenirken hata");
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

        if (SelectedStatus != "Tümü")
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
