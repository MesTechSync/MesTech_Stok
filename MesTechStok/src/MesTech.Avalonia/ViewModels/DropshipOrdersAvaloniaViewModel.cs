#pragma warning disable CS1998
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Siparisler ViewModel — siparis DataGrid + durum filtre + tedarikci iletme.
/// </summary>
public partial class DropshipOrdersAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedStatus = "Tumu";

    public ObservableCollection<DropshipOrderItemDto> Orders { get; } = [];
    public ObservableCollection<string> StatusOptions { get; } = ["Tumu", "Yeni", "Tedarikçiye İletildi", "Kargoda", "Teslim Edildi", "İptal"];

    private List<DropshipOrderItemDto> _allOrders = [];

    public DropshipOrdersAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var result = await _mediator.Send(new GetDropshipOrdersQuery(_currentUser.TenantId));

            _allOrders = result.Select(o => new DropshipOrderItemDto
            {
                OrderId = o.SupplierOrderRef ?? o.Id.ToString("N")[..8].ToUpper(),
                Customer = string.Empty,
                Supplier = o.DropshipSupplierId.ToString("N")[..8],
                Status = o.Status,
                CustomerPrice = 0m,
                SupplierPrice = 0m,
                NetProfit = 0m
            }).ToList();

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
