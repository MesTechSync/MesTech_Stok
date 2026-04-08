using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class OpenCartAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // Connection / KPI
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private int productCount;
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private decimal dailyRevenue;
    [ObservableProperty] private string syncStatus = "Bekliyor";
    [ObservableProperty] private string lastSyncTime = "-";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<PlatformOrderItem> _allOrders = [];

    // Store selector
    [ObservableProperty] private string selectedStore = string.Empty;
    public ObservableCollection<string> Stores { get; } = [];

    // Active tab index (0=Ürünler, 1=Siparişler, 2=Kategoriler, 3=Ayarlar)
    [ObservableProperty] private int selectedTabIndex;

    // Tab 1 — Ürünler
    public ObservableCollection<OpenCartProductItem> Products { get; } = [];

    // Tab 2 — Siparişler
    public ObservableCollection<OpenCartOrderItem> Orders { get; } = [];

    // Tab 3 — Kategoriler (platform ↔ MesTech mapping)
    public ObservableCollection<OpenCartCategoryMappingItem> CategoryMappings { get; } = [];

    // Tab 4 — Ayarlar
    [ObservableProperty] private string storeUrl = string.Empty;
    [ObservableProperty] private string apiKey = string.Empty;
    [ObservableProperty] private string connectionTestResult = string.Empty;
    [ObservableProperty] private bool connectionTestSuccess;
    [ObservableProperty] private bool connectionTestRan;

    // Legacy compat (used by old AXAML binding)
    public ObservableCollection<PlatformOrderItem> RecentOrders { get; } = [];

    public OpenCartAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetPlatformDashboardQuery(_currentUser.TenantId, PlatformType.OpenCart), ct) ?? new PlatformDashboardDto();
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
            IsEmpty = result.ProductCount == 0 && result.OrderCount == 0;

            // Load store selector from real data
            var stores = await _mediator.Send(new GetStoresByTenantQuery(_currentUser.TenantId), ct);
            Stores.Clear();
            foreach (var s in stores.Where(s => s.PlatformType == PlatformType.OpenCart))
                Stores.Add(!string.IsNullOrEmpty(s.StoreName) ? s.StoreName : $"OpenCart #{s.Id.ToString()[..8]}");
            if (Stores.Count > 0 && string.IsNullOrEmpty(SelectedStore))
                SelectedStore = Stores[0];

            // Load Products tab via MediatR
            var store = stores.FirstOrDefault(s => s.PlatformType == PlatformType.OpenCart && s.IsActive);
            if (store is not null)
            {
                var products = await _mediator.Send(new GetOpenCartProductsQuery(_currentUser.TenantId, store.Id), ct);
                Products.Clear();
                foreach (var p in products.Products)
                    Products.Add(new OpenCartProductItem
                    {
                        Sku = p.SKU,
                        Name = p.Name,
                        Price = p.Price,
                        Stock = p.Quantity,
                        InMesTech = true
                    });
            }
        }, "OpenCart verileri yuklenirken hata");
    }

    // ── Search Filter ────────────────────────────────────────────────────────
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
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Sync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new SyncPlatformCommand(
                "OPENCART", MesTech.Domain.Enums.SyncDirection.Bidirectional));
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

    [RelayCommand]
    private async Task TestConnection()
    {
        ConnectionTestRan = false;
        IsLoading = true;
        try
        {
            var stores = await _mediator.Send(new GetStoresByTenantQuery(_currentUser.TenantId));
            var store = stores.FirstOrDefault(s => s.PlatformType == PlatformType.OpenCart && s.IsActive);

            if (store is null)
            {
                ConnectionTestSuccess = false;
                ConnectionTestResult = "OpenCart magazasi bulunamadi — once magaza ekleyin.";
                ConnectionTestRan = true;
                return;
            }

            var result = await _mediator.Send(new TestStoreConnectionCommand(store.Id));
            ConnectionTestSuccess = result.IsSuccess;
            ConnectionTestResult = result.IsSuccess
                ? $"Baglanti basarili! ({(int)result.ResponseTime.TotalMilliseconds} ms)"
                : $"Baglanti hatasi: {result.ErrorMessage ?? "API ulasilamiyor"}";
            ConnectionTestRan = true;
        }
        catch (Exception ex)
        {
            ConnectionTestSuccess = false;
            ConnectionTestResult = $"Baglanti hatasi: {ex.Message}";
            ConnectionTestRan = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class OpenCartProductItem
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool InMesTech { get; set; }
    public string InMesTechDisplay => InMesTech ? "✅" : "❌";
}

public class OpenCartOrderItem
{
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class OpenCartCategoryMappingItem
{
    public string PlatformCategory { get; set; } = string.Empty;
    public string MesTechCategory { get; set; } = string.Empty;
    public bool IsMapped { get; set; }
    public string MappedDisplay => IsMapped ? "✅ Eslendi" : "❌ Eslemedi";
}
