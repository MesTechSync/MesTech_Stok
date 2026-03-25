using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Ayarlari ViewModel — kimlik bilgileri, sync araligi, webhook, oto-sync toggle.
/// </summary>
public partial class StoreSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


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

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // TODO: Replace with _mediator.Send(new GetStoreSettingsQuery()) when handler ready
            await Task.Delay(100);
            IsEmpty = true;
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
            // TODO: Replace with _mediator.Send(new UpdateStoreSettingsCommand(...)) when handler ready
            await Task.Delay(100);
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
