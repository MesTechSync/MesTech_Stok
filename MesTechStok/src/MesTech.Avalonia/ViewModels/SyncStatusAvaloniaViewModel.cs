using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Platform Sync Status screen.
/// Displays marketplace sync status with per-row sync actions.
/// Will be wired to GetPlatformSyncStatusQuery via MediatR when full migration starts.
/// </summary>
public partial class SyncStatusAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<SyncStatusItemDto> Items { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(80); // Simulate async load

            Items.Clear();
            Items.Add(new SyncStatusItemDto { PlatformAdi = "Trendyol", SonSenkronizasyon = "2026-03-17 09:45", Durum = "Basarili", UrunSayisi = 1_245, SiparisSayisi = 87 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "Hepsiburada", SonSenkronizasyon = "2026-03-17 09:30", Durum = "Basarili", UrunSayisi = 980, SiparisSayisi = 52 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "CicekSepeti", SonSenkronizasyon = "2026-03-17 08:15", Durum = "Hatali", UrunSayisi = 560, SiparisSayisi = 23 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "N11", SonSenkronizasyon = "2026-03-17 09:40", Durum = "Basarili", UrunSayisi = 720, SiparisSayisi = 31 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "Amazon", SonSenkronizasyon = "2026-03-16 23:00", Durum = "Bekliyor", UrunSayisi = 2_100, SiparisSayisi = 145 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "eBay", SonSenkronizasyon = "2026-03-17 07:20", Durum = "Hatali", UrunSayisi = 430, SiparisSayisi = 12 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "Shopify", SonSenkronizasyon = "2026-03-17 09:50", Durum = "Basarili", UrunSayisi = 1_890, SiparisSayisi = 98 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "WooCommerce", SonSenkronizasyon = "2026-03-17 09:48", Durum = "Basarili", UrunSayisi = 670, SiparisSayisi = 28 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "Pazarama", SonSenkronizasyon = "2026-03-16 18:30", Durum = "Bekliyor", UrunSayisi = 310, SiparisSayisi = 9 });
            Items.Add(new SyncStatusItemDto { PlatformAdi = "PttAVM", SonSenkronizasyon = "2026-03-17 06:00", Durum = "Basarili", UrunSayisi = 245, SiparisSayisi = 15 });

            TotalCount = Items.Count;
            IsEmpty = Items.Count == 0;
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
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SyncPlatform(SyncStatusItemDto? platform)
    {
        if (platform is null) return;

        platform.Durum = "Bekliyor";
        // Force UI refresh by removing and re-adding
        var index = Items.IndexOf(platform);
        if (index >= 0)
        {
            Items.RemoveAt(index);
            Items.Insert(index, platform);
        }

        await Task.Delay(500); // Simulate sync

        platform.Durum = "Basarili";
        platform.SonSenkronizasyon = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        index = Items.IndexOf(platform);
        if (index >= 0)
        {
            Items.RemoveAt(index);
            Items.Insert(index, platform);
        }
    }
}

public class SyncStatusItemDto
{
    public string PlatformAdi { get; set; } = string.Empty;
    public string SonSenkronizasyon { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public int UrunSayisi { get; set; }
    public int SiparisSayisi { get; set; }

    /// <summary>Color code based on status for UI binding.</summary>
    public string DurumRenk => Durum switch
    {
        "Basarili" => "#16A34A",
        "Hatali" => "#DC2626",
        "Bekliyor" => "#D97706",
        _ => "#64748B"
    };
}
