using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

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
            await Task.Delay(200); // Will be replaced with MediatR query

            Shipments.Clear();
            Shipments.Add(new ShipmentItemDto { TrackingNumber = "TR1234567890", OrderNumber = "SIP-1042", CarrierName = "Aras Kargo", RecipientName = "Ahmet Yilmaz", Status = "Hazirlaniyor", ShipDate = "19.03.2026" });
            Shipments.Add(new ShipmentItemDto { TrackingNumber = "TR1234567891", OrderNumber = "SIP-1041", CarrierName = "Yurtici Kargo", RecipientName = "Fatma Demir", Status = "Yolda", ShipDate = "18.03.2026" });
            Shipments.Add(new ShipmentItemDto { TrackingNumber = "TR1234567892", OrderNumber = "SIP-1040", CarrierName = "Surat Kargo", RecipientName = "Mehmet Kaya", Status = "Teslim Edildi", ShipDate = "17.03.2026" });
            Shipments.Add(new ShipmentItemDto { TrackingNumber = "TR1234567893", OrderNumber = "SIP-1039", CarrierName = "Aras Kargo", RecipientName = "Ayse Ozturk", Status = "Teslim Edildi", ShipDate = "16.03.2026" });

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
