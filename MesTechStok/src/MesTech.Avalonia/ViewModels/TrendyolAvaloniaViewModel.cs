using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// WPF013 — Trendyol bağlantı test paneli.
/// Connection test, API info, quick actions, call log, rate limit display.
/// </summary>
public partial class TrendyolAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

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

    public TrendyolAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(100, CancellationToken);
            // MediatR queries will be wired when CQRS handlers are available
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

    // ── WPF013: Test Connection ───────────────────────────────────────────────
    [RelayCommand]
    private async Task TestConnection()
    {
        if (IsConnectionTesting) return;
        IsConnectionTesting = true;
        ConnectionStatusText = "Test ediliyor...";
        ConnectionStatusColor = "#F59E0B"; // amber

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(Random.Shared.Next(300, 900), CancellationToken);
            sw.Stop();

            PingDurationMs = (int)sw.ElapsedMilliseconds;
            LastPingTime = DateTime.Now.ToString("HH:mm:ss");
            SonBaglantiZamani = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            // Simulate 90% success rate
            bool success = Random.Shared.NextDouble() > 0.1;
            IsConnected = success;

            if (success)
            {
                ConnectionStatusText = $"Bağlantı başarılı ({PingDurationMs} ms)";
                ConnectionStatusColor = "#10B981"; // emerald
                AddLog("GET", "/integration/product/sellers/{sellerId}/products", "200 OK", PingDurationMs);
            }
            else
            {
                ConnectionStatusText = "Bağlantı başarısız — API ulaşılamıyor";
                ConnectionStatusColor = "#EF4444"; // red
                AddLog("GET", "/integration/product/sellers/{sellerId}/products", "503 Hata", PingDurationMs);
            }

            // Update rate limit mock
            RateLimitUsed = Math.Min(RateLimitUsed + 1, RateLimitTotal);
            RateLimitRemaining = RateLimitTotal - RateLimitUsed;
            RateLimitPercent = (double)RateLimitUsed / RateLimitTotal * 100.0;
        }
        catch (OperationCanceledException)
        {
            ConnectionStatusText = "Test iptal edildi";
            ConnectionStatusColor = "#6B7280";
        }
        finally
        {
            IsConnectionTesting = false;
        }
    }

    // ── WPF013: Fetch Products ────────────────────────────────────────────────
    [RelayCommand]
    private async Task FetchProducts()
    {
        if (IsQuickActionRunning) return;
        IsQuickActionRunning = true;
        QuickActionStatus = "Ürünler çekiliyor...";

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(Random.Shared.Next(400, 1200), CancellationToken);
            sw.Stop();
            int duration = (int)sw.ElapsedMilliseconds;

            var mockProducts = new[]
            {
                "iPhone 15 Pro Kılıf - Siyah",
                "Samsung Galaxy S24 Ekran Koruyucu",
                "AirPods Pro Şarj Kablosu",
                "Laptop Standı Alüminyum",
                "Mekanik Klavye TKL RGB"
            };

            ProductCount = 5;
            QuickActionStatus = $"5 ürün çekildi ({duration} ms)";
            AddLog("GET", "/integration/product/sellers/{sellerId}/products?page=0&size=5", "200 OK", duration);

            RateLimitUsed = Math.Min(RateLimitUsed + 1, RateLimitTotal);
            RateLimitRemaining = RateLimitTotal - RateLimitUsed;
            RateLimitPercent = (double)RateLimitUsed / RateLimitTotal * 100.0;
        }
        catch (OperationCanceledException)
        {
            QuickActionStatus = "İstek iptal edildi";
        }
        finally
        {
            IsQuickActionRunning = false;
        }
    }

    // ── WPF013: Fetch Orders ─────────────────────────────────────────────────
    [RelayCommand]
    private async Task FetchOrders()
    {
        if (IsQuickActionRunning) return;
        IsQuickActionRunning = true;
        QuickActionStatus = "Siparişler çekiliyor...";

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(Random.Shared.Next(400, 1200), CancellationToken);
            sw.Stop();
            int duration = (int)sw.ElapsedMilliseconds;

            RecentOrders.Clear();
            var statuses = new[] { "Paketlendi", "Kargoda", "Teslim Edildi", "Beklemede", "Onaylandı" };
            for (int i = 1; i <= 5; i++)
            {
                RecentOrders.Add(new PlatformOrderItem(
                    $"TY-2024-{100 + i:D6}",
                    DateTime.Now.AddHours(-i * 3).ToString("dd.MM.yyyy HH:mm"),
                    $"Müşteri {i}",
                    $"₺{Random.Shared.Next(150, 3500):N0}",
                    statuses[i - 1]
                ));
            }

            TotalCount = RecentOrders.Count;
            OrderCount = RecentOrders.Count;
            IsEmpty = false;
            QuickActionStatus = $"5 sipariş çekildi ({duration} ms)";
            AddLog("GET", "/order/sellers/{sellerId}/orders?status=Created&size=5", "200 OK", duration);

            RateLimitUsed = Math.Min(RateLimitUsed + 1, RateLimitTotal);
            RateLimitRemaining = RateLimitTotal - RateLimitUsed;
            RateLimitPercent = (double)RateLimitUsed / RateLimitTotal * 100.0;
        }
        catch (OperationCanceledException)
        {
            QuickActionStatus = "İstek iptal edildi";
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
            await Task.Delay(100, CancellationToken);
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
