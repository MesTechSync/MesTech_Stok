using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Senkronizasyon Durumu ViewModel — platform bazli sync durumu + saglik gostergesi.
/// </summary>
public partial class PlatformSyncStatusAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<PlatformSyncStatusItemDto> Platforms { get; } = [];

    public PlatformSyncStatusAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(250);

            Platforms.Clear();
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "Trendyol", PlatformColor = "#FF6F00", StoreCount = 2, LastSync = "19.03.2026 14:30", LastSuccess = "19.03.2026 14:30", ErrorsToday = 0, HealthStatus = "Saglikli" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "Hepsiburada", PlatformColor = "#FF6000", StoreCount = 1, LastSync = "19.03.2026 14:25", LastSuccess = "19.03.2026 14:25", ErrorsToday = 1, HealthStatus = "Saglikli" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "N11", PlatformColor = "#0B2441", StoreCount = 1, LastSync = "19.03.2026 14:20", LastSuccess = "19.03.2026 14:20", ErrorsToday = 0, HealthStatus = "Saglikli" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "Ciceksepeti", PlatformColor = "#F27A1A", StoreCount = 1, LastSync = "18.03.2026 22:00", LastSuccess = "18.03.2026 20:00", ErrorsToday = 5, HealthStatus = "Uyari" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "Amazon", PlatformColor = "#FF9900", StoreCount = 1, LastSync = "19.03.2026 11:30", LastSuccess = "19.03.2026 11:30", ErrorsToday = 0, HealthStatus = "Saglikli" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "eBay", PlatformColor = "#E53238", StoreCount = 0, LastSync = "-", LastSuccess = "-", ErrorsToday = 0, HealthStatus = "Pasif" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "Shopify", PlatformColor = "#96BF48", StoreCount = 1, LastSync = "19.03.2026 14:00", LastSuccess = "19.03.2026 14:00", ErrorsToday = 0, HealthStatus = "Saglikli" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "WooCommerce", PlatformColor = "#96588A", StoreCount = 1, LastSync = "19.03.2026 13:30", LastSuccess = "19.03.2026 13:30", ErrorsToday = 2, HealthStatus = "Uyari" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "Pazarama", PlatformColor = "#00B8D4", StoreCount = 0, LastSync = "-", LastSuccess = "-", ErrorsToday = 0, HealthStatus = "Pasif" });
            Platforms.Add(new PlatformSyncStatusItemDto { Platform = "PttAVM", PlatformColor = "#FFD600", StoreCount = 1, LastSync = "19.03.2026 12:00", LastSuccess = "19.03.2026 12:00", ErrorsToday = 0, HealthStatus = "Saglikli" });

            TotalCount = Platforms.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Platform senkronizasyon durumu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncPlatformAsync(PlatformSyncStatusItemDto? platform)
    {
        if (platform is null || platform.HealthStatus == "Pasif") return;

        platform.HealthStatus = "Senkronize ediliyor...";
        var index = Platforms.IndexOf(platform);
        if (index >= 0) { Platforms.RemoveAt(index); Platforms.Insert(index, platform); }

        await Task.Delay(1000);

        platform.HealthStatus = "Saglikli";
        platform.LastSync = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        platform.LastSuccess = platform.LastSync;
        platform.ErrorsToday = 0;
        if (index >= 0) { Platforms.RemoveAt(index); Platforms.Insert(index, platform); }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PlatformSyncStatusItemDto
{
    public string Platform { get; set; } = string.Empty;
    public string PlatformColor { get; set; } = "#0078D4";
    public int StoreCount { get; set; }
    public string LastSync { get; set; } = string.Empty;
    public string LastSuccess { get; set; } = string.Empty;
    public int ErrorsToday { get; set; }
    public string HealthStatus { get; set; } = string.Empty;

    public string HealthColor => HealthStatus switch
    {
        "Saglikli" => "#4CAF50",
        "Uyari" => "#FF9800",
        "Hatali" => "#F44336",
        "Pasif" => "#9E9E9E",
        _ => "#64748B"
    };
}
