using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
using MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Yonetimi ViewModel — magaza listesi + platform ayarlari.
/// MediatR wired: GetStoreSettingsQuery.
/// </summary>
public partial class StoreManagementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private StoreItemDto? selectedStore;

    public ObservableCollection<StoreItemDto> Stores { get; } = [];

    public StoreManagementAvaloniaViewModel(
        IMediator mediator, ITenantProvider tenantProvider,
        INavigationService nav, IDialogService dialog)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        _nav = nav;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var settings = await _mediator.Send(new GetStoreSettingsQuery(tenantId), ct);

            Stores.Clear();
            foreach (var s in settings.Stores)
            {
                Stores.Add(new StoreItemDto
                {
                    StoreName = s.StoreName,
                    Platform = s.PlatformType,
                    ApiStatus = s.IsActive ? (s.HasCredentials ? "Bagli" : "Kimlik Eksik") : "Pasif",
                });
            }

            TotalCount = Stores.Count;
            IsEmpty = TotalCount == 0;
        }, "Magaza bilgileri yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // HH-DEV2-028: Navigate to StoreWizard for adding new store
    [RelayCommand]
    private async Task AddStore() => await _nav.NavigateToAsync("StoreWizard");

    // HH-DEV2-028: Navigate to StoreSettings for editing selected store
    [RelayCommand]
    private async Task EditStore()
    {
        if (SelectedStore is null) return;
        await _nav.NavigateToAsync("StoreSettings");
    }

    // HH-DEV2-028: Delete selected store with confirmation
    [RelayCommand]
    private async Task DeleteStore()
    {
        if (SelectedStore is null) return;

        var confirmed = await _dialog.ConfirmAsync(
            "Magaza Sil",
            $"{SelectedStore.StoreName} magazasini silmek istediginize emin misiniz?");

        if (!confirmed) return;

        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            await _mediator.Send(new DeleteStoreCredentialCommand(
                tenantId, SelectedStore.Platform, SelectedStore.StoreName), ct);

            Stores.Remove(SelectedStore);
            SelectedStore = null;
            TotalCount = Stores.Count;
            IsEmpty = TotalCount == 0;
        }, "Magaza silinirken hata");
    }
}

public class StoreItemDto
{
    public string StoreName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string ApiStatus { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public string LastSync { get; set; } = string.Empty;
}
