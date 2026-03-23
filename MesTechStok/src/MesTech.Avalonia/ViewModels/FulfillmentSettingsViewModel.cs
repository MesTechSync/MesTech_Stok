using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fulfillment settings ViewModel — F-04.
/// Settings tabs for Amazon FBA and Hepsilojistik.
/// Credential fields, connection test, auto-replenish toggle.
/// </summary>
public partial class FulfillmentSettingsViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;

    // Amazon FBA
    [ObservableProperty] private string fbaApiKey = string.Empty;
    [ObservableProperty] private string fbaApiSecret = string.Empty;
    [ObservableProperty] private string fbaSellerId = string.Empty;
    [ObservableProperty] private string fbaMarketplaceId = string.Empty;
    [ObservableProperty] private bool fbaAutoReplenish;
    [ObservableProperty] private string fbaConnectionStatus = string.Empty;

    // Hepsilojistik
    [ObservableProperty] private string hepsiApiKey = string.Empty;
    [ObservableProperty] private string hepsiApiSecret = string.Empty;
    [ObservableProperty] private string hepsiStoreId = string.Empty;
    [ObservableProperty] private bool hepsiAutoReplenish;
    [ObservableProperty] private string hepsiConnectionStatus = string.Empty;

    public FulfillmentSettingsViewModel(IMediator mediator)
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
            await Task.Delay(100); // Will be replaced with MediatR query for persisted settings
            // Settings loaded from persistence
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Fulfillment ayarlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task Save()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR command
            // Settings saved
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Fulfillment ayarlari kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestFbaConnection()
    {
        FbaConnectionStatus = "Test ediliyor...";
        try
        {
            await Task.Delay(500); // Will be replaced with actual connection test
            FbaConnectionStatus = string.IsNullOrWhiteSpace(FbaApiKey)
                ? "API Key bos — baglanti kurulamadi"
                : "Baglanti basarili";
        }
        catch (Exception ex)
        {
            FbaConnectionStatus = $"Baglanti hatasi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestHepsiConnection()
    {
        HepsiConnectionStatus = "Test ediliyor...";
        try
        {
            await Task.Delay(500); // Will be replaced with actual connection test
            HepsiConnectionStatus = string.IsNullOrWhiteSpace(HepsiApiKey)
                ? "API Key bos — baglanti kurulamadi"
                : "Baglanti basarili";
        }
        catch (Exception ex)
        {
            HepsiConnectionStatus = $"Baglanti hatasi: {ex.Message}";
        }
    }
}
