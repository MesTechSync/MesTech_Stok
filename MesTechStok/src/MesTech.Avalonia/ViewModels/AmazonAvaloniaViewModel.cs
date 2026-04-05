using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class AmazonAvaloniaViewModel : ViewModelBase
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

    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false;

    public ObservableCollection<PlatformOrderItem> RecentOrders { get; } = [];

    public AmazonAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetPlatformDashboardQuery(_currentUser.TenantId, PlatformType.Amazon), ct) ?? new PlatformDashboardDto();
            IsConnected = result.IsConnected;
            ProductCount = result.ProductCount;
            OrderCount = result.OrderCount;
            DailyRevenue = result.DailyRevenue;
            SyncStatus = result.SyncStatus;
            LastSyncTime = result.LastSyncAt?.ToString("HH:mm") ?? "-";
            _allOrders.Clear();
            foreach (var o in result.RecentOrders)
                _allOrders.Add(new PlatformOrderItem(o.OrderNumber, o.OrderDate.ToString("dd.MM.yyyy"), o.CustomerName, o.Total.ToString("N2"), o.Status, PlatformType.Amazon));
            ApplyFilter();
        }, "Amazon verileri yuklenirken hata");
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
        filtered = SortColumn switch
        {
            "OrderNumber"  => SortAscending ? filtered.OrderBy(x => x.OrderNumber)  : filtered.OrderByDescending(x => x.OrderNumber),
            "OrderDate"    => SortAscending ? filtered.OrderBy(x => x.OrderDate)    : filtered.OrderByDescending(x => x.OrderDate),
            "CustomerName" => SortAscending ? filtered.OrderBy(x => x.CustomerName) : filtered.OrderByDescending(x => x.CustomerName),
            "TotalAmount"  => SortAscending ? filtered.OrderBy(x => x.TotalAmount)  : filtered.OrderByDescending(x => x.TotalAmount),
            "Status"       => SortAscending ? filtered.OrderBy(x => x.Status)       : filtered.OrderByDescending(x => x.Status),
            _              => SortAscending ? filtered.OrderBy(x => x.OrderDate)    : filtered.OrderByDescending(x => x.OrderDate),
        };

        foreach (var item in filtered) RecentOrders.Add(item);
        TotalCount = RecentOrders.Count;
        IsEmpty = RecentOrders.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Sync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new SyncPlatformCommand("Amazon", SyncDirection.Bidirectional));
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
