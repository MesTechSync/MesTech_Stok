using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Detay ViewModel — magaza bilgileri + sync gecmisi + adapter saglik durumu.
/// </summary>
public partial class StoreDetailAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // Store info
    [ObservableProperty] private string storeName = string.Empty;
    [ObservableProperty] private string platformName = string.Empty;
    [ObservableProperty] private string apiStatus = string.Empty;
    [ObservableProperty] private int productCount;
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private string adapterHealth = string.Empty;
    [ObservableProperty] private string adapterVersion = string.Empty;

    public ObservableCollection<SyncHistoryItemDto> SyncHistory { get; } = [];

    public StoreDetailAvaloniaViewModel(IMediator mediator)
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
            // DEP: DEV1 — Replace with GetStoreDetailQuery via MediatR (DEV 1 handler gerekli)

            StoreName = "Ana Magaza - Trendyol";
            PlatformName = "Trendyol";
            ApiStatus = "Bagli";
            ProductCount = 1250;
            OrderCount = 89;
            AdapterHealth = "Saglikli";
            AdapterVersion = "v2.1.0";

            SyncHistory.Clear();
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 14:30", Status = "Basarili", ProductsSynced = 1250, Errors = 0 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 14:00", Status = "Basarili", ProductsSynced = 1248, Errors = 2 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 13:30", Status = "Basarili", ProductsSynced = 1248, Errors = 0 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 13:00", Status = "Hatali", ProductsSynced = 0, Errors = 1 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 12:30", Status = "Basarili", ProductsSynced = 1245, Errors = 0 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 12:00", Status = "Basarili", ProductsSynced = 1245, Errors = 0 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 11:30", Status = "Basarili", ProductsSynced = 1242, Errors = 3 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 11:00", Status = "Basarili", ProductsSynced = 1240, Errors = 0 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 10:30", Status = "Basarili", ProductsSynced = 1240, Errors = 0 });
            SyncHistory.Add(new SyncHistoryItemDto { SyncTime = "19.03.2026 10:00", Status = "Basarili", ProductsSynced = 1238, Errors = 1 });

            IsEmpty = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Magaza detaylari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class SyncHistoryItemDto
{
    public string SyncTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProductsSynced { get; set; }
    public int Errors { get; set; }
}
