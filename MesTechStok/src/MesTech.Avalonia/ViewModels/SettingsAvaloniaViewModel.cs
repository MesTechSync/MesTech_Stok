using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Features.Settings.Commands.SaveApiSettings;
using MesTech.Application.Features.Settings.Commands.TestApiConnection;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using MesTech.Application.Queries.GetStoresByTenant;
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
        await SafeExecuteAsync(async ct =>
        {
            var creds = await _mediator.Send(new GetCredentialsSettingsQuery(_currentUser.TenantId), ct);
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

            // G540 orphan: general + profile settings
            try { _ = await _mediator.Send(new GetGeneralSettingsQuery(_currentUser.TenantId), ct); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetGeneralSettings failed: {ex.Message}"); }
            try { _ = await _mediator.Send(new GetProfileSettingsQuery(_currentUser.TenantId), ct); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetProfileSettings failed: {ex.Message}"); }
        }, "Ayarlar yuklenirken hata");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsConnectionTested = false;
        ConnectionMessage = string.Empty;
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new TestApiConnectionCommand(
                _currentUser.TenantId, ApiUrl));
            ConnectionSuccess = result.IsSuccess;
            ConnectionMessage = result.IsSuccess
                ? $"Baglanti basarili! ({result.ResponseTimeMs}ms, HTTP {result.StatusCode})"
                : $"Baglanti basarisiz: {result.Message}";

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
            var result = await _mediator.Send(new SaveApiSettingsCommand(
                _currentUser.TenantId,
                ApiUrl,
                null,
                60,
                true));
            IsSaved = result.IsSuccess;
            if (!result.IsSuccess)
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "API ayarlari kaydedilemedi.";
            }
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
            var stores = await _mediator.Send(new GetStoresByTenantQuery(_currentUser.TenantId));
            var store = stores.FirstOrDefault(s =>
                s.StoreName.Contains(SelectedCredential.Platform, StringComparison.OrdinalIgnoreCase) && s.IsActive);

            if (store is null)
            {
                await _dialog.ShowInfoAsync($"{SelectedCredential.Platform}: Magaza bulunamadi — once magaza ekleyin.", "Test Sonucu");
                return;
            }

            var result = await _mediator.Send(new TestStoreConnectionCommand(store.Id)) ?? new();
            SelectedCredential.IsConnected = result.IsSuccess;
            SelectedCredential.Status = result.IsSuccess ? "Bagli" : "Baglanti hatasi";

            var msg = result.IsSuccess
                ? $"{SelectedCredential.Platform}: Baglanti basarili ({(int)result.ResponseTime.TotalMilliseconds} ms)"
                : $"{SelectedCredential.Platform}: {result.ErrorMessage ?? "API ulasilamiyor"}";
            await _dialog.ShowInfoAsync(msg, "Test Sonucu");
        }
        catch (Exception ex)
        {
            await _dialog.ShowInfoAsync($"Test hatasi: {ex.Message}", "Baglanti Testi");
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
