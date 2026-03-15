using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Marketplaces screen — Dalga 11.
/// Will display 10+ platform adapters with sync status and configuration.
/// </summary>
public partial class MarketplacesAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = "Pazaryeri yonetimi ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int platformCount = 10;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            PlatformCount = 10;
            Summary = "Pazaryeri yonetimi ekrani hazir. 10 platform entegrasyonu, API ayarlari, senkronizasyon durumu ve hata loglari burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
