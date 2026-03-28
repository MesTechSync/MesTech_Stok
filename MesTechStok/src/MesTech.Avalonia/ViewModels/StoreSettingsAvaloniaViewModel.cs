using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Ayarlari ViewModel — wired to GetStoreSettingsQuery via MediatR.
/// </summary>
public partial class StoreSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string storeName = string.Empty;
    [ObservableProperty] private string platformName = string.Empty;
    [ObservableProperty] private string companyName = string.Empty;
    [ObservableProperty] private string? taxNumber;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? address;
    [ObservableProperty] private int storeCount;
    [ObservableProperty] private string apiKey = string.Empty;
    [ObservableProperty] private string apiSecret = string.Empty;
    [ObservableProperty] private string sellerId = string.Empty;
    [ObservableProperty] private int syncIntervalMinutes = 15;
    [ObservableProperty] private string webhookUrl = string.Empty;
    [ObservableProperty] private bool autoSyncEnabled;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveSuccess;

    public StoreSettingsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Magaza Ayarlari";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetStoreSettingsQuery(_currentUser.TenantId), ct);

            CompanyName = result.CompanyName;
            TaxNumber = result.TaxNumber;
            Phone = result.Phone;
            Email = result.Email;
            Address = result.Address;
            StoreCount = result.Stores.Count;

            if (result.Stores.Count > 0)
            {
                StoreName = result.Stores[0].StoreName;
                PlatformName = result.Stores[0].PlatformType;
            }

            IsEmpty = result.Stores.Count == 0;
        }, "Magaza ayarlari yuklenirken hata");
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        HasError = false;
        SaveSuccess = false;
        try
        {
            await Task.Delay(100, CancellationToken);
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
