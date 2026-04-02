using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class EbayAvaloniaViewModel : ViewModelBase
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
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<PlatformOrderItem> _allOrders = [];

    public ObservableCollection<PlatformOrderItem> RecentOrders { get; } = [];

    public EbayAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var result = await _mediator.Send(new GetPlatformDashboardQuery(_currentUser.TenantId, PlatformType.eBay)) ?? new PlatformDashboardDto();
            IsConnected = result.IsConnected;
            ProductCount = result.ProductCount;
            OrderCount = result.OrderCount;
            DailyRevenue = result.DailyRevenue;
            SyncStatus = result.SyncStatus;
            LastSyncTime = result.LastSyncAt?.ToString("HH:mm") ?? "-";
            _allOrders.Clear();
            foreach (var o in result.RecentOrders)
                _allOrders.Add(new PlatformOrderItem(o.OrderNumber, o.OrderDate.ToString("dd.MM.yyyy"), o.CustomerName, o.Total.ToString("N2"), o.Status));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"eBay verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        RecentOrders.Clear();
        var filtered = _allOrders.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(o =>
                o.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var item in filtered) RecentOrders.Add(item);
        TotalCount = RecentOrders.Count;
        IsEmpty = RecentOrders.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Sync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(
                new TriggerSyncCommand(_currentUser.TenantId, "eBay"), CancellationToken);
            SyncStatus = result.IsSuccess ? "Tamamlandi" : $"Hata: {result.ErrorMessage}";
            LastSyncTime = DateTime.Now.ToString("HH:mm");
            if (result.IsSuccess)
                await LoadAsync();
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
