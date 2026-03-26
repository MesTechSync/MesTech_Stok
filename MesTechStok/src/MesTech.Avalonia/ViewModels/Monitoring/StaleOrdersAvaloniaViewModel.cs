using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Monitoring;

/// <summary>
/// Gecikmiş sipariş monitoring ViewModel — wired to GetStaleOrdersQuery via MediatR.
/// 48+ saat gönderilmemiş siparişleri listeler (Chain 11).
/// </summary>
public partial class StaleOrdersAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<StaleOrderItem> StaleOrders { get; } = [];

    public int TotalStaleCount => StaleOrders.Count;
    public int Warning48hCount => StaleOrders.Count(o => o.ElapsedHours >= 48 && o.ElapsedHours < 72);
    public int Critical72hCount => StaleOrders.Count(o => o.ElapsedHours >= 72);

    public StaleOrdersAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(
                new GetStaleOrdersQuery(_tenantProvider.GetCurrentTenantId()));

            StaleOrders.Clear();
            foreach (var o in result)
            {
                StaleOrders.Add(new StaleOrderItem(
                    o.OrderNumber,
                    o.Platform?.ToString() ?? "-",
                    o.CustomerName ?? "-",
                    o.CreatedAt));
            }

            OnPropertyChanged(nameof(TotalStaleCount));
            OnPropertyChanged(nameof(Warning48hCount));
            OnPropertyChanged(nameof(Critical72hCount));
            IsEmpty = StaleOrders.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class StaleOrderItem
{
    public StaleOrderItem(string orderNumber, string platform, string customerName, DateTime orderDate)
    {
        OrderNumber = orderNumber;
        Platform = platform;
        CustomerName = customerName;
        OrderDate = orderDate;
        ElapsedHours = (DateTime.UtcNow - orderDate).TotalHours;
    }

    public string OrderNumber { get; }
    public string Platform { get; }
    public string CustomerName { get; }
    public DateTime OrderDate { get; }
    public double ElapsedHours { get; }

    public string OrderDateText => OrderDate.ToString("dd.MM.yyyy HH:mm");
    public string ElapsedText => ElapsedHours switch
    {
        < 72 => $"{(int)ElapsedHours} saat",
        < 168 => $"{(int)(ElapsedHours / 24)} gun",
        _ => $"{(int)(ElapsedHours / 24)} gun"
    };
    public string SeverityText => ElapsedHours switch
    {
        < 72 => "Uyari",
        < 120 => "Kritik",
        _ => "Acil"
    };
}
