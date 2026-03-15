using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Settings screen — Dalga 10.
/// Will provide tenant config, user preferences, and integration settings.
/// </summary>
public partial class SettingsAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = "Ayarlar ekrani — Dalga 10 sonrasi aktif edilecek.";
    [ObservableProperty] private string appVersion = "MesTech Stok v10.0 — Avalonia PoC";

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            Summary = "Ayarlar ekrani hazir. Tenant yapilandirmasi, kullanici tercihleri ve entegrasyon ayarlari burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
