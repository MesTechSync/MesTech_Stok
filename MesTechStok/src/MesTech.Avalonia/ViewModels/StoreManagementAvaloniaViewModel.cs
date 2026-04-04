using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
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

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<StoreItemDto> Stores { get; } = [];

    public StoreManagementAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
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
}

public class StoreItemDto
{
    public string StoreName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string ApiStatus { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public string LastSync { get; set; } = string.Empty;
}
