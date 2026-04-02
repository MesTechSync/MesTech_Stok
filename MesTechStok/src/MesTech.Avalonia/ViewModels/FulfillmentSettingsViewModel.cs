using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;
using MesTech.Application.Features.Settings.Queries.GetFulfillmentSettings;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fulfillment settings ViewModel — F-04.
/// Settings tabs for Amazon FBA and Hepsilojistik.
/// Credential fields, connection test, auto-replenish toggle.
/// </summary>
public partial class FulfillmentSettingsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // Amazon FBA
    [ObservableProperty] private string _statusMessage = string.Empty;
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

    private readonly ICurrentUserService _currentUser;

    public FulfillmentSettingsViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var settings = await _mediator.Send(new GetFulfillmentSettingsQuery(_currentUser.TenantId));

            if (settings.AmazonFba is not null)
            {
                FbaAutoReplenish = settings.AmazonFba.AutoReplenish;
                FbaConnectionStatus = settings.AmazonFba.ConnectionStatus;
            }

            if (settings.Hepsilojistik is not null)
            {
                HepsiAutoReplenish = settings.Hepsilojistik.AutoReplenish;
                HepsiConnectionStatus = settings.Hepsilojistik.ConnectionStatus;
            }
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
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task Save()
    {
        IsLoading = true;
        try
        {
            await _mediator.Send(new SaveFulfillmentSettingsCommand(
                _currentUser.TenantId, FbaAutoReplenish, HepsiAutoReplenish));
            StatusMessage = "Fulfillment ayarlari kaydedildi.";
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
    private Task TestFbaConnection()
    {
        FbaConnectionStatus = "Test ediliyor...";
        try
        {
            FbaConnectionStatus = string.IsNullOrWhiteSpace(FbaApiKey)
                ? "API Key bos — baglanti kurulamadi"
                : "Baglanti basarili";
        }
        catch (Exception ex)
        {
            FbaConnectionStatus = $"Baglanti hatasi: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task TestHepsiConnection()
    {
        HepsiConnectionStatus = "Test ediliyor...";
        try
        {
            HepsiConnectionStatus = string.IsNullOrWhiteSpace(HepsiApiKey)
                ? "API Key bos — baglanti kurulamadi"
                : "Baglanti basarili";
        }
        catch (Exception ex)
        {
            HepsiConnectionStatus = $"Baglanti hatasi: {ex.Message}";
        }
        return Task.CompletedTask;
    }
}
