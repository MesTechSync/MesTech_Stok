using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Ayarlari ViewModel — kimlik bilgileri, sync araligi, webhook, oto-sync toggle.
/// TODO: Replace demo data with MediatR.Send(new UpdateStoreSettingsCommand()) when A1 CQRS is ready.
/// </summary>
public partial class StoreSettingsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string storeName = string.Empty;
    [ObservableProperty] private string platformName = string.Empty;
    [ObservableProperty] private string apiKey = string.Empty;
    [ObservableProperty] private string apiSecret = string.Empty;
    [ObservableProperty] private string sellerId = string.Empty;
    [ObservableProperty] private string webhookUrl = string.Empty;
    [ObservableProperty] private int syncIntervalMinutes = 30;
    [ObservableProperty] private bool autoSyncEnabled = true;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveSuccess;

    public StoreSettingsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // TODO: Replace with MediatR.Send(new GetStoreSettingsQuery()) when A1 CQRS is ready
            await Task.Delay(200);

            StoreName = "Ana Magaza - Trendyol";
            PlatformName = "Trendyol";
            ApiKey = "tr-api-key-****";
            ApiSecret = "****";
            SellerId = "123456";
            WebhookUrl = "https://api.mestech.com/webhooks/trendyol";
            SyncIntervalMinutes = 30;
            AutoSyncEnabled = true;
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
    private async Task SaveAsync()
    {
        IsSaving = true;
        HasError = false;
        SaveSuccess = false;
        try
        {
            // TODO: Replace with MediatR.Send(new UpdateStoreSettingsCommand()) when A1 CQRS is ready
            await Task.Delay(500);
            SaveSuccess = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Ayarlar kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
