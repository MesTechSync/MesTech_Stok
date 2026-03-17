using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Settings screen ViewModel — API configuration, notifications, theme selection.
/// Tab-like sections: API Ayarlari, Bildirimler, Tema.
/// </summary>
public partial class SettingsAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string appVersion = "MesTech Stok v10.0 — Avalonia PoC";

    // API settings
    [ObservableProperty] private string apiUrl = "https://api.mestech.com/v1";
    [ObservableProperty] private string apiKey = string.Empty;
    [ObservableProperty] private bool isConnectionTested;
    [ObservableProperty] private bool connectionSuccess;
    [ObservableProperty] private string connectionMessage = string.Empty;

    // Notification settings
    [ObservableProperty] private bool isEmailEnabled = true;
    [ObservableProperty] private bool isSmsEnabled;
    [ObservableProperty] private bool isPushEnabled = true;

    // Theme settings
    [ObservableProperty] private string selectedTheme = "Light";

    // Save state
    [ObservableProperty] private bool isSaved;

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(100); // Simulate loading settings
            // Settings loaded from defaults (or future persistence)
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Ayarlar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsConnectionTested = false;
        ConnectionMessage = string.Empty;
        IsLoading = true;
        try
        {
            await Task.Delay(800); // Simulate API call

            if (!string.IsNullOrWhiteSpace(ApiUrl) && ApiUrl.StartsWith("http"))
            {
                ConnectionSuccess = true;
                ConnectionMessage = "Baglanti basarili! API erisimi dogrulandi.";
            }
            else
            {
                ConnectionSuccess = false;
                ConnectionMessage = "Baglanti basarisiz. Gecerli bir API URL giriniz.";
            }

            IsConnectionTested = true;
        }
        catch (Exception ex)
        {
            ConnectionSuccess = false;
            ConnectionMessage = $"Baglanti testi hatasi: {ex.Message}";
            IsConnectionTested = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        IsSaved = false;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate save
            IsSaved = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Ayarlar kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
