using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderList;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Siparis Listesi ViewModel — DataGrid + arama + filtre.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class OrderListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<OrderListItemDto> Orders { get; } = [];

    public OrderListAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetOrderListQuery(Guid.Empty));

            Orders.Clear();
            foreach (var item in result)
            {
                Orders.Add(new OrderListItemDto
                {
                    OrderNumber = item.OrderNumber,
                    CustomerName = item.CustomerName ?? string.Empty,
                    Platform = item.SourcePlatform ?? string.Empty,
                    OrderDate = item.OrderDate.ToString("dd.MM.yyyy"),
                    TotalAmount = item.TotalAmount,
                    Status = item.Status
                });
            }

            TotalCount = Orders.Count;
            IsEmpty = TotalCount == 0;
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

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class OrderListItemDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
