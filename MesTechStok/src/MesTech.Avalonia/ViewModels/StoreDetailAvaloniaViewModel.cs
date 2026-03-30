using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Stores.Queries.GetStoreDetail;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Detay ViewModel — magaza bilgileri + sync gecmisi + adapter saglik durumu.
/// Wired to GetStoreDetailQuery via MediatR.
/// </summary>
public partial class StoreDetailAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // Store info
    [ObservableProperty] private string storeName = string.Empty;
    [ObservableProperty] private string platformName = string.Empty;
    [ObservableProperty] private string apiStatus = string.Empty;
    [ObservableProperty] private int productCount;
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private string adapterHealth = string.Empty;
    [ObservableProperty] private string adapterVersion = string.Empty;

    public ObservableCollection<SyncHistoryItemDto> SyncHistory { get; } = [];

    public StoreDetailAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var stores = await _mediator.Send(new GetStoresByTenantQuery(_currentUser.TenantId));
            var store = stores.FirstOrDefault(s => s.IsActive);

            if (store is null)
            {
                IsEmpty = true;
                StoreName = "Magaza bulunamadi";
                return;
            }

            var detail = await _mediator.Send(new GetStoreDetailQuery(_currentUser.TenantId, store.Id));
            if (detail is null)
            {
                IsEmpty = true;
                StoreName = "Detay yuklenemedi";
                return;
            }

            StoreName = detail.Name;
            PlatformName = detail.Platform;
            ApiStatus = detail.CredentialStatus == "Configured" ? "Bagli" : "Yapilandirilmamis";
            ProductCount = detail.ProductCount;
            AdapterHealth = detail.IsActive ? "Saglikli" : "Pasif";
            AdapterVersion = detail.WebhookStatus;

            IsEmpty = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Magaza detaylari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class SyncHistoryItemDto
{
    public string SyncTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProductsSynced { get; set; }
    public int Errors { get; set; }
}
