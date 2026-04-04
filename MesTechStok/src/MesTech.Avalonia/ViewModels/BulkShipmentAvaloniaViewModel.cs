using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Shipping.Commands.CreateShipment;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class BulkShipmentAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private bool isSending;
    [ObservableProperty] private int selectedCount;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int progressCurrent;
    [ObservableProperty] private int progressTotal;
    [ObservableProperty] private double progressPercent;
    [ObservableProperty] private int successCount;
    [ObservableProperty] private int failCount;
    [ObservableProperty] private int warningCount;
    [ObservableProperty] private string selectedProvider = "YurticiKargo";
    [ObservableProperty] private bool selectAll;
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<BulkShipmentItemDto> _allOrders = [];

    public ObservableCollection<BulkShipmentItemDto> Orders { get; } = [];
    public ObservableCollection<string> Providers { get; } =
    [
        "YurticiKargo", "ArasKargo", "SuratKargo", "MngKargo", "PttKargo", "Hepsijet", "Sendeo"
    ];

    public BulkShipmentAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allOrders
            : _allOrders.Where(o =>
                o.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Orders.Clear();
        foreach (var o in filtered)
            Orders.Add(o);

        TotalCount = Orders.Count;
        UpdateSelectedCount();
        IsEmpty = TotalCount == 0;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async _ =>
        {
            _allOrders.Clear();
            _allOrders.Add(new() { IsSelected = true, OrderNumber = "SIP-1001", CustomerName = "Ahmet Yilmaz", City = "Istanbul", Weight = 1.2m, Status = "Bekliyor" });
            _allOrders.Add(new() { IsSelected = true, OrderNumber = "SIP-1002", CustomerName = "Mehmet Kaya", City = "Ankara", Weight = 0.8m, Status = "Bekliyor" });
            _allOrders.Add(new() { IsSelected = false, OrderNumber = "SIP-1003", CustomerName = "Ayse Demir", City = "Izmir", Weight = 3.5m, Status = "Adres eksik", HasWarning = true });
            _allOrders.Add(new() { IsSelected = true, OrderNumber = "SIP-1004", CustomerName = "Fatma Sahin", City = "Bursa", Weight = 1.0m, Status = "Bekliyor" });
            _allOrders.Add(new() { IsSelected = true, OrderNumber = "SIP-1005", CustomerName = "Ali Ozturk", City = "Antalya", Weight = 2.3m, Status = "Bekliyor" });

            ApplyFilter();
        }, "Siparis listesi yuklenirken hata");
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Orders.Count(o => o.IsSelected);
    }

    partial void OnSelectAllChanged(bool value)
    {
        foreach (var order in Orders)
        {
            if (!order.HasWarning)
                order.IsSelected = value;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    private async Task SendBulkAsync()
    {
        var selected = Orders.Where(o => o.IsSelected && !o.HasWarning).ToList();
        if (selected.Count == 0) return;

        IsSending = true;
        ProgressTotal = selected.Count;
        ProgressCurrent = 0;
        SuccessCount = 0;
        FailCount = 0;
        WarningCount = 0;

        foreach (var order in selected)
        {
            ProgressCurrent++;
            ProgressPercent = (double)ProgressCurrent / ProgressTotal * 100;

            try
            {
                var provider = Enum.TryParse<CargoProvider>(SelectedProvider, out var cp)
                    ? cp : CargoProvider.YurticiKargo;
                var result = await _mediator.Send(new CreateShipmentCommand(
                    _currentUser.TenantId,
                    order.OrderId,
                    provider,
                    order.CustomerName,
                    order.City,
                    order.CustomerName, // phone placeholder — view doesn't collect phone
                    order.Weight));
                order.Status = result.IsSuccess ? "Gonderildi" : $"Hata: {result.ErrorMessage}";
                order.TrackingNumber = result.TrackingNumber;
                if (result.IsSuccess) SuccessCount++; else FailCount++;
            }
            catch
            {
                order.Status = "Hata";
                FailCount++;
            }
        }

        IsSending = false;
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}

public class BulkShipmentItemDto : ObservableObject
{
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string City { get; set; } = "";
    public decimal Weight { get; set; }

    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public bool HasWarning { get; set; }
    public string? TrackingNumber { get; set; }
}
