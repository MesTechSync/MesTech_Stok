using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Platform.Queries.GetSyncHistory;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Senkronizasyon Gecmisi — tum platformlar veya tek platform filtreli.
/// GetSyncHistoryQuery + TriggerSyncCommand wired.
/// </summary>
public partial class PlatformSyncHistoryAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int successCount;
    [ObservableProperty] private int failCount;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string syncMessage = string.Empty;
    [ObservableProperty] private bool isSyncing;

    public ObservableCollection<MesTech.Application.Features.Platform.Queries.GetSyncHistory.SyncHistoryItemDto> SyncHistory { get; } = [];

    public ObservableCollection<string> Platforms { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon",
        "eBay", "Ozon", "PttAVM", "Etsy", "Shopify", "WooCommerce",
        "Zalando", "Pazarama", "OpenCart", "Bitrix24"
    ];

    public PlatformSyncHistoryAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var platformFilter = SelectedPlatform == "Tumu" ? null : SelectedPlatform;
            var result = await _mediator.Send(
                new GetSyncHistoryQuery(tenantId, PlatformFilter: platformFilter, Count: 50), CancellationToken);

            SyncHistory.Clear();
            foreach (var item in result)
                SyncHistory.Add(item);

            TotalCount = SyncHistory.Count;
            SuccessCount = SyncHistory.Count(s => s.IsSuccess);
            FailCount = SyncHistory.Count(s => !s.IsSuccess);
            IsEmpty = SyncHistory.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Senkronizasyon gecmisi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedPlatformChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task TriggerSyncAsync()
    {
        if (SelectedPlatform == "Tumu")
        {
            SyncMessage = "Tek platform secin.";
            return;
        }

        IsSyncing = true;
        SyncMessage = string.Empty;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(
                new TriggerSyncCommand(tenantId, SelectedPlatform), CancellationToken);

            SyncMessage = result.IsSuccess
                ? $"{SelectedPlatform} senkronizasyonu baslatildi (Job: {result.JobId})"
                : $"Hata: {result.ErrorMessage}";

            await LoadAsync();
        }
        catch (Exception ex)
        {
            SyncMessage = $"Senkronizasyon hatasi: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
