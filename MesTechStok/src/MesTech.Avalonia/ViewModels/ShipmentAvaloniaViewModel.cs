using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kargo Takip ViewModel — sevkiyat listesi + durum badge.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class ShipmentAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int deliveredCount;
    [ObservableProperty] private int inTransitCount;
    [ObservableProperty] private int preparingCount;

    public ObservableCollection<ShipmentItemDto> Shipments { get; } = [];

    public ShipmentAvaloniaViewModel(IMediator mediator)
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
            var result = await _mediator.Send(new GetShipmentCostsQuery(Guid.Empty));

            Shipments.Clear();
            foreach (var s in result)
            {
                Shipments.Add(new ShipmentItemDto
                {
                    TrackingNumber = s.TrackingNumber ?? s.Id.ToString("N")[..12].ToUpper(),
                    OrderNumber = s.OrderId.ToString("N")[..8].ToUpper(),
                    CarrierName = s.Provider.ToString(),
                    RecipientName = string.Empty,
                    Status = s.IsChargedToCustomer ? "Teslim Edildi" : "Hazirlaniyor",
                    ShipDate = s.ShippedAt.ToString("dd.MM.yyyy")
                });
            }

            TotalCount = Shipments.Count;
            DeliveredCount = Shipments.Count(s => s.Status == "Teslim Edildi");
            InTransitCount = Shipments.Count(s => s.Status == "Yolda");
            PreparingCount = Shipments.Count(s => s.Status == "Hazirlaniyor");
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Gonderi bilgileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ShipmentItemDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ShipDate { get; set; } = string.Empty;
}
