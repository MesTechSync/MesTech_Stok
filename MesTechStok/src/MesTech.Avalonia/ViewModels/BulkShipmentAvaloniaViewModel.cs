using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

public partial class BulkShipmentAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

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

    public ObservableCollection<BulkShipmentItemDto> Orders { get; } = [];
    public ObservableCollection<string> Providers { get; } =
    [
        "YurticiKargo", "ArasKargo", "SuratKargo", "MngKargo", "PttKargo", "Hepsijet", "Sendeo"
    ];

    public BulkShipmentAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        try
        {
            await Task.Delay(200);

            Orders.Clear();
            Orders.Add(new() { IsSelected = true, OrderNumber = "SIP-1001", CustomerName = "Ahmet Yilmaz", City = "Istanbul", Weight = 1.2m, Status = "Bekliyor" });
            Orders.Add(new() { IsSelected = true, OrderNumber = "SIP-1002", CustomerName = "Mehmet Kaya", City = "Ankara", Weight = 0.8m, Status = "Bekliyor" });
            Orders.Add(new() { IsSelected = false, OrderNumber = "SIP-1003", CustomerName = "Ayse Demir", City = "Izmir", Weight = 3.5m, Status = "Adres eksik", HasWarning = true });
            Orders.Add(new() { IsSelected = true, OrderNumber = "SIP-1004", CustomerName = "Fatma Sahin", City = "Bursa", Weight = 1.0m, Status = "Bekliyor" });
            Orders.Add(new() { IsSelected = true, OrderNumber = "SIP-1005", CustomerName = "Ali Ozturk", City = "Antalya", Weight = 2.3m, Status = "Bekliyor" });

            TotalCount = Orders.Count;
            UpdateSelectedCount();
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Siparis listesi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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
                await Task.Delay(500); // Simulate API call
                order.Status = "Gonderildi";
                order.TrackingNumber = $"TR{DateTime.Now:yyyyMMdd}{ProgressCurrent:D4}";
                SuccessCount++;
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
    private async Task Refresh() => await LoadAsync();
}

public class BulkShipmentItemDto : ObservableObject
{
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string City { get; set; } = "";
    public decimal Weight { get; set; }

    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public bool HasWarning { get; set; }
    public string? TrackingNumber { get; set; }
}
