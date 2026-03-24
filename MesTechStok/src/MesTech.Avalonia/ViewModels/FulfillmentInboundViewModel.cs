using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fulfillment Inbound shipment list ViewModel — F-02.
/// DataGrid: ShipmentId, Provider, Status, ItemCount, CreatedDate, TrackingNumber.
/// Filters: Provider, Status, DateRange. Uses GetFulfillmentOrdersQuery.
/// </summary>
public partial class FulfillmentInboundViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedProvider = "Tumu";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private DateTimeOffset? dateFrom;
    [ObservableProperty] private DateTimeOffset? dateTo;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<InboundShipmentDto> Shipments { get; } = [];

    public ObservableCollection<string> Providers { get; } =
    [
        "Tumu", "Amazon FBA", "Hepsilojistik", "Trendyol Fulfillment", "Kendi Depomuz"
    ];

    public ObservableCollection<string> Statuses { get; } =
    [
        "Tumu", "Hazirlaniyor", "Gonderildi", "Teslim Alindi", "Isleniyor", "Tamamlandi", "Iptal"
    ];

    private List<InboundShipmentDto> _allShipments = [];

    public FulfillmentInboundViewModel(IMediator mediator)
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
            var centers = new[] { FulfillmentCenter.AmazonFBA, FulfillmentCenter.Hepsilojistik, FulfillmentCenter.TrendyolFulfillment };
            var allResults = new List<InboundShipmentDto>();

            foreach (var center in centers)
            {
                var query = new GetFulfillmentOrdersQuery(center, DateTime.UtcNow.AddDays(-90));
                var orders = await _mediator.Send(query);

                foreach (var order in orders)
                {
                    allResults.Add(new InboundShipmentDto
                    {
                        ShipmentId = order.OrderId,
                        Provider = center.ToString(),
                        Status = order.Status,
                        ItemCount = order.Items.Count,
                        CreatedDate = order.ShippedDate?.ToString("dd.MM.yyyy") ?? "-",
                        TrackingNumber = order.TrackingNumber ?? "-"
                    });
                }
            }

            _allShipments = allResults;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Inbound verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allShipments.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(s =>
                s.ShipmentId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.TrackingNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedProvider != "Tumu")
        {
            filtered = filtered.Where(s => s.Provider.Contains(SelectedProvider, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatus != "Tumu")
        {
            filtered = filtered.Where(s => s.Status.Contains(SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        Shipments.Clear();
        foreach (var item in filtered)
            Shipments.Add(item);

        TotalCount = Shipments.Count;
        IsEmpty = Shipments.Count == 0 && !HasError;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void CreateInbound()
    {
        // Dialog will be opened by the view via IDialogService
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allShipments.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedProviderChanged(string value)
    {
        if (_allShipments.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        if (_allShipments.Count > 0)
            ApplyFilters();
    }
}

public class InboundShipmentDto
{
    public string ShipmentId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
}
