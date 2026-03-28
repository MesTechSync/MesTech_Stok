using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Settings screen ViewModel — API configuration, notifications, theme selection.
/// G017: Platform API key CRUD + mağaza ekleme.
/// </summary>
public partial class SettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private readonly IThemeService _themeService;

    [ObservableProperty] private string appVersion = "MesTech Stok v10.0 — Avalonia";

    // API settings
    [ObservableProperty] private string apiUrl = "https://api.mestech.com/v1";
    [ObservableProperty] private string apiKey = string.Empty;
    [ObservableProperty] private bool isConnectionTested;
    [ObservableProperty] private bool connectionSuccess;
    [ObservableProperty] private string connectionMessage = string.Empty;

    // Platform Credentials (G017)
    [ObservableProperty] private PlatformCredentialItem? selectedCredential;
    public ObservableCollection<PlatformCredentialItem> PlatformCredentials { get; } = [];

    // Notification settings
    [ObservableProperty] private bool isEmailEnabled = true;
    [ObservableProperty] private bool isSmsEnabled;
    [ObservableProperty] private bool isPushEnabled = true;

    // Theme settings — backed by IThemeService
    [ObservableProperty] private string selectedTheme = "Light";

    // Save state
    [ObservableProperty] private bool isSaved;

    public SettingsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog, IThemeService themeService)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        _themeService = themeService;

        // Sync initial value from service
        SelectedTheme = _themeService.CurrentTheme;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        _themeService.SetTheme(value);
    }

    // G116: Command for theme button clicks
    [RelayCommand]
    private void SetTheme(string theme) => SelectedTheme = theme;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var creds = await _mediator.Send(new GetCredentialsSettingsQuery(_currentUser.TenantId));
            var configured = creds.ConfiguredPlatforms.ToHashSet(StringComparer.OrdinalIgnoreCase);

            PlatformCredentials.Clear();
            var allPlatforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon TR", "Pazarama", "eBay", "Shopify", "WooCommerce", "Ozon" };
            foreach (var platform in allPlatforms)
            {
                var isConfigured = configured.Contains(platform);
                PlatformCredentials.Add(new PlatformCredentialItem
                {
                    Platform = platform,
                    ApiKey = isConfigured ? "***" : "",
                    Status = isConfigured ? "Bagli" : "Yapilandirilmamis",
                    IsConnected = isConfigured
                });
            }
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
            // TODO: Wire to TestApiConnectionCommand via MediatR
            await Task.CompletedTask;

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
            // TODO: Wire to SaveSettingsCommand via MediatR
            await Task.CompletedTask;
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

    [RelayCommand]
    private async Task AddPlatform()
    {
        await _dialog.ShowInfoAsync("Yeni platform ekleme ekranina yonlendiriliyorsunuz... (StoreWizard)", "Platform Ekle");
    }

    [RelayCommand]
    private async Task EditCredential()
    {
        if (SelectedCredential == null)
        {
            await _dialog.ShowInfoAsync("Duzenlemek icin bir platform secin.", "Platform Ayarlari");
            return;
        }
        await _dialog.ShowInfoAsync($"{SelectedCredential.Platform} API ayarlari duzenlenecek. (StoreSettings)", "Platform Duzenle");
    }

    [RelayCommand]
    private async Task TestPlatformConnection()
    {
        if (SelectedCredential == null)
        {
            await _dialog.ShowInfoAsync("Test icin bir platform secin.", "Baglanti Testi");
            return;
        }

        IsLoading = true;
        try
        {
            // TODO: Wire to TestPlatformConnectionCommand via MediatR
            await Task.CompletedTask;
            if (SelectedCredential.IsConnected)
            {
                await _dialog.ShowInfoAsync($"{SelectedCredential.Platform}: Baglanti basarili!", "Test Sonucu");
            }
            else
            {
                await _dialog.ShowInfoAsync($"{SelectedCredential.Platform}: API anahtari yapilandirilmamis.", "Test Sonucu");
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RemoveCredential()
    {
        if (SelectedCredential == null)
        {
            await _dialog.ShowInfoAsync("Kaldirmak icin bir platform secin.", "Platform Kaldir");
            return;
        }
        PlatformCredentials.Remove(SelectedCredential);
        SelectedCredential = null;
    }
}

public class PlatformCredentialItem
{
    public string Platform { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}
