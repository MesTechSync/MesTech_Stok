using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class ZalandoAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private bool isConnected;

    [ObservableProperty] private int productCount;
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private decimal dailyRevenue;
    [ObservableProperty] private string syncStatus = "Bekliyor";
    [ObservableProperty] private string lastSyncTime = "-";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<PlatformOrderItem> RecentOrders { get; } = [];

    public ZalandoAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var result = await _mediator.Send(new GetPlatformDashboardQuery(_currentUser.TenantId, PlatformType.Zalando));
            IsConnected = result.IsConnected;
            ProductCount = result.ProductCount;
            OrderCount = result.OrderCount;
            DailyRevenue = result.DailyRevenue;
            SyncStatus = result.SyncStatus;
            LastSyncTime = result.LastSyncAt?.ToString("HH:mm") ?? "-";
            RecentOrders.Clear();
            foreach (var o in result.RecentOrders)
                RecentOrders.Add(new PlatformOrderItem(o.OrderNumber, o.OrderDate.ToString("dd.MM.yyyy"), o.CustomerName, o.Total.ToString("N2"), o.Status, PlatformType.Zalando));
            TotalCount = RecentOrders.Count;
            IsEmpty = RecentOrders.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Zalando verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Sync()
    {
        IsLoading = true;
        try
        {

            var result = await _mediator.Send(new SyncPlatformCommand("Zalando", SyncDirection.Bidirectional));
            if (result.IsSuccess)
            {
                SyncStatus = $"Tamamlandi ({result.ItemsProcessed} urun)";
                LastSyncTime = DateTime.Now.ToString("HH:mm");
                await LoadAsync();
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Senkronizasyon basarisiz";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Senkronizasyon hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
