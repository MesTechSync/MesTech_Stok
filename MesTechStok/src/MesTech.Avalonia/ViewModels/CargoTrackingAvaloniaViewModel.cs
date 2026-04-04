using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cargo Tracking screen — wired to GetCargoTrackingListQuery via MediatR.
/// Displays cargo shipments with firm-based filtering.
/// </summary>
public partial class CargoTrackingAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedFirm = "Tümü";

    private readonly List<CargoTrackingItemDto> _allItems = [];

    public ObservableCollection<CargoTrackingItemDto> Shipments { get; } = [];

    public ObservableCollection<string> Firms { get; } =
    [
        "Tümü",
        "Yurtiçi Kargo",
        "Aras Kargo",
        "Sürat Kargo"
    ];

    public CargoTrackingAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetCargoTrackingListQuery(_tenantProvider.GetCurrentTenantId(), 100), ct);

            _allItems.Clear();
            _allItems.AddRange(result.Select(c => new CargoTrackingItemDto
            {
                TakipNo = c.TrackingNumber ?? c.OrderNumber,
                Firma = c.CargoProvider ?? "-",
                Tarih = c.ShippedAt ?? DateTime.MinValue,
                Durum = c.Status switch
                {
                    "Delivered" => "Teslim Edildi",
                    "Shipped" => "Yolda",
                    _ => "Hazirlaniyor"
                },
                Alici = c.OrderNumber
            }));

            ApplyFilter();
        }, "Kargo verileri yuklenirken hata");
    }

    partial void OnSelectedFirmChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Shipments.Clear();
        var filtered = SelectedFirm == "Tümü"
            ? _allItems
            : _allItems.Where(s => s.Firma == SelectedFirm).ToList();

        foreach (var item in filtered)
            Shipments.Add(item);

        TotalCount = Shipments.Count;
        IsEmpty = Shipments.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class CargoTrackingItemDto
{
    public string TakipNo { get; set; } = string.Empty;
    public string Firma { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string Alici { get; set; } = string.Empty;
}
