using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stock Management ViewModel — wired to GetStockSummaryQuery via MediatR.
/// </summary>
public partial class StockAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private int totalProducts;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int inStockProducts;
    [ObservableProperty] private int outOfStockProducts;
    [ObservableProperty] private int lowStockProducts;
    [ObservableProperty] private decimal totalStockValue;
    [ObservableProperty] private int totalUnits;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private bool isSyncing;
    [ObservableProperty] private string syncStatus = string.Empty;

    public StockAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Stok Yonetimi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetStockSummaryQuery(_currentUser.TenantId), ct);

            TotalProducts = result.TotalProducts;
            InStockProducts = result.InStockProducts;
            OutOfStockProducts = result.OutOfStockProducts;
            LowStockProducts = result.LowStockProducts;
            TotalStockValue = result.TotalStockValue;
            TotalUnits = result.TotalUnits;
            TotalCount = TotalProducts;
            Summary = $"{TotalProducts} urun — {TotalStockValue:N2} ₺ deger";
            IsEmpty = TotalProducts == 0;
        }, "Stok ozeti yuklenirken hata");
    }

    [RelayCommand]
    private async Task AddMovement()
    {
        await _dialog.ShowInfoAsync("Stok hareketi ekle yakinda aktif olacak.", "MesTech");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // HH-DEV2-017: Platform stock sync
    [RelayCommand]
    private async Task SyncPlatformStock()
    {
        var confirmed = await _dialog.ShowConfirmAsync(
            "Tum platformlara stok gonderilecek. Devam etmek istiyor musunuz?",
            "Stok Senkronizasyonu");
        if (!confirmed) return;

        IsSyncing = true;
        SyncStatus = "Platformlara stok gonderiyor...";

        try
        {
            var result = await _mediator.Send(new TriggerSyncCommand(
                _currentUser.TenantId, "stock"), CancellationToken);

            SyncStatus = result.IsSuccess
                ? "Stok senkronizasyonu tamamlandi."
                : $"Hata: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            SyncStatus = $"Senkronizasyon hatasi: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }
}
