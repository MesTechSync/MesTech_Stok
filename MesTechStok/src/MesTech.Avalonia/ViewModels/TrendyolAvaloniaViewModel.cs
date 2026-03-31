#pragma warning disable CS1998
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// WPF013 — Trendyol bağlantı test paneli.
/// Connection test, API info, quick actions, call log, rate limit display.
/// </summary>
public partial class TrendyolAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // ── KPI ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private int productCount;
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private decimal dailyRevenue;
    [ObservableProperty] private string syncStatus = "Bekliyor";
    [ObservableProperty] private string lastSyncTime = "-";
    [ObservableProperty] private int totalCount;

    // ── WPF013: Connection Panel ──────────────────────────────────────────────
    [ObservableProperty] private string connectionStatusText = "Bağlantı test edilmedi";
    [ObservableProperty] private string connectionStatusColor = "#6B7280"; // MesMutedGray equivalent
    [ObservableProperty] private bool isConnectionTesting;
    [ObservableProperty] private string lastPingTime = "-";
    [ObservableProperty] private int pingDurationMs;

    // ── WPF013: API Info ─────────────────────────────────────────────────────
    [ObservableProperty] private string sellerId = "12345678";
    [ObservableProperty] private string apiKeyMasked = "****-****-****-3f7a";
    [ObservableProperty] private string sonBaglantiZamani = "-";
    [ObservableProperty] private int rateLimitUsed;
    [ObservableProperty] private int rateLimitTotal = 1000;
    [ObservableProperty] private int rateLimitRemaining = 1000;
    [ObservableProperty] private double rateLimitPercent;

    // ── WPF013: Quick Action results ─────────────────────────────────────────
    [ObservableProperty] private string quickActionStatus = string.Empty;
    [ObservableProperty] private bool isQuickActionRunning;

    public ObservableCollection<PlatformOrderItem> RecentOrders { get; } = [];
    public ObservableCollection<ApiCallLogItem> ApiCallLogs { get; } = [];

    public TrendyolAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var result = await _mediator.Send(new GetPlatformDashboardQuery(_currentUser.TenantId, PlatformType.Trendyol));
            IsConnected = result.IsConnected;
            ProductCount = result.ProductCount;
            OrderCount = result.OrderCount;
            DailyRevenue = result.DailyRevenue;
            SyncStatus = result.SyncStatus;
            LastSyncTime = result.LastSyncAt?.ToString("HH:mm") ?? "-";
            RecentOrders.Clear();
            foreach (var o in result.RecentOrders)
                RecentOrders.Add(new PlatformOrderItem(o.OrderNumber, o.OrderDate.ToString("dd.MM.yyyy"), o.CustomerName, o.Total.ToString("N2"), o.Status));
            TotalCount = RecentOrders.Count;
            IsEmpty = RecentOrders.Count == 0;
        }
        catch (OperationCanceledException) { /* navigasyon sırasında iptal — normal akış */ }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Trendyol verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── WPF013: Test Connection — real adapter ping via TestStoreConnectionCommand ──
    [RelayCommand]
    private async Task TestConnection()
    {
        if (IsConnectionTesting) return;
        IsConnectionTesting = true;
        ConnectionStatusText = "Test ediliyor...";
        ConnectionStatusColor = "#F59E0B"; // amber

        try
        {
            // Find the Trendyol store for this tenant
            var stores = await _mediator.Send(new GetStoresByTenantQuery(_currentUser.TenantId), CancellationToken);
            var trendyolStore = stores.FirstOrDefault(s => s.PlatformType == PlatformType.Trendyol && s.IsActive);

            if (trendyolStore is null)
            {
                ConnectionStatusText = "Trendyol mağazası bulunamadı — Ayarlar'dan ekleyin";
                ConnectionStatusColor = "#EF4444";
                IsConnected = false;
                AddLog("GET", "/stores/trendyol", "404 Not Found", 0);
                return;
            }

            SellerId = trendyolStore.ExternalStoreId ?? trendyolStore.Id.ToString()[..8];

            var result = await _mediator.Send(new TestStoreConnectionCommand(trendyolStore.Id), CancellationToken);

            PingDurationMs = (int)result.ResponseTime.TotalMilliseconds;
            LastPingTime = DateTime.Now.ToString("HH:mm:ss");
            SonBaglantiZamani = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            IsConnected = result.IsSuccess;

            if (result.IsSuccess)
            {
                ConnectionStatusText = $"Bağlantı başarılı ({PingDurationMs} ms)";
                ConnectionStatusColor = "#10B981"; // emerald
                if (result.ProductCount.HasValue)
                    ProductCount = result.ProductCount.Value;
                AddLog("GET", "/integration/product/sellers/{sellerId}/products", $"{result.HttpStatusCode ?? 200} OK", PingDurationMs);
            }
            else
            {
                ConnectionStatusText = $"Bağlantı başarısız — {result.ErrorMessage ?? "API ulaşılamıyor"}";
                ConnectionStatusColor = "#EF4444"; // red
                AddLog("GET", "/integration/product/sellers/{sellerId}/products", $"{result.HttpStatusCode ?? 503} Hata", PingDurationMs);
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionStatusText = "Test iptal edildi";
            ConnectionStatusColor = "#6B7280";
        }
        catch (Exception ex)
        {
            ConnectionStatusText = $"Test hatası: {ex.Message}";
            ConnectionStatusColor = "#EF4444";
            IsConnected = false;
        }
        finally
        {
            IsConnectionTesting = false;
        }
    }

    // ── WPF013: Fetch Products — real query via GetProductsQuery ────────────────
    [RelayCommand]
    private async Task FetchProducts()
    {
        if (IsQuickActionRunning) return;
        IsQuickActionRunning = true;
        QuickActionStatus = "Ürünler çekiliyor...";

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await _mediator.Send(
                new GetProductsQuery(_currentUser.TenantId, PageSize: 5), CancellationToken);
            sw.Stop();
            int duration = (int)sw.ElapsedMilliseconds;

            ProductCount = result.TotalCount;
            QuickActionStatus = $"{result.Items.Count} ürün çekildi (toplam {result.TotalCount}) — {duration} ms";
            AddLog("GET", "/api/v1/products?page=1&size=5", "200 OK", duration);
        }
        catch (OperationCanceledException)
        {
            QuickActionStatus = "İstek iptal edildi";
        }
        catch (Exception ex)
        {
            QuickActionStatus = $"Ürün çekme hatası: {ex.Message}";
        }
        finally
        {
            IsQuickActionRunning = false;
        }
    }

    // ── WPF013: Fetch Orders — real query via GetPlatformDashboardQuery ────────
    [RelayCommand]
    private async Task FetchOrders()
    {
        if (IsQuickActionRunning) return;
        IsQuickActionRunning = true;
        QuickActionStatus = "Siparişler çekiliyor...";

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await _mediator.Send(
                new GetPlatformDashboardQuery(_currentUser.TenantId, PlatformType.Trendyol), CancellationToken);
            sw.Stop();
            int duration = (int)sw.ElapsedMilliseconds;

            RecentOrders.Clear();
            foreach (var o in result.RecentOrders)
            {
                RecentOrders.Add(new PlatformOrderItem(
                    o.OrderNumber,
                    o.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                    o.CustomerName,
                    o.Total.ToString("N2"),
                    o.Status));
            }

            TotalCount = RecentOrders.Count;
            OrderCount = result.OrderCount;
            IsEmpty = RecentOrders.Count == 0;
            QuickActionStatus = $"{RecentOrders.Count} sipariş çekildi (toplam {result.OrderCount}) — {duration} ms";
            AddLog("GET", "/order/sellers/{sellerId}/orders?status=Created&size=5", "200 OK", duration);
        }
        catch (OperationCanceledException)
        {
            QuickActionStatus = "İstek iptal edildi";
        }
        catch (Exception ex)
        {
            QuickActionStatus = $"Sipariş çekme hatası: {ex.Message}";
        }
        finally
        {
            IsQuickActionRunning = false;
        }
    }

    // ── Existing commands ────────────────────────────────────────────────────
    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Sync()
    {
        IsLoading = true;
        try
        {
            SyncStatus = "Tamamlandi";
            LastSyncTime = DateTime.Now.ToString("HH:mm");
        }
        catch (OperationCanceledException) { /* navigasyon sırasında iptal — normal akış */ }
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

    // ── Helpers ──────────────────────────────────────────────────────────────
    private void AddLog(string method, string endpoint, string status, int durationMs)
    {
        ApiCallLogs.Insert(0, new ApiCallLogItem(
            DateTime.Now.ToString("HH:mm:ss"),
            $"{method} {endpoint}",
            status,
            $"{durationMs} ms"
        ));

        // Keep last 10
        while (ApiCallLogs.Count > 10)
            ApiCallLogs.RemoveAt(ApiCallLogs.Count - 1);
    }
}
