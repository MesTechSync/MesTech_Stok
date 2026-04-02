using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Domain.Interfaces;
using SkiaSharp;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia Dashboard — 8 KPI cards + 6 premium KPI cards + son siparişler + kritik stok DataGrid.
/// Demo data'dan gerçek MediatR GetDashboardSummaryQuery'ye dönüştürüldü (EMR-03-v2 ALAN-D).
/// İ-03 GÖREV 4: Auto-refresh (30s DispatcherTimer) + premium KPI card VMs + platform statuses + AI insight.
/// </summary>
public partial class DashboardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    // ── Auto-refresh timer ────────────────────────────────────────────────
    private DispatcherTimer? _refreshTimer;
    private DateTime _lastRefresh;

    // ── Satır 1: Ana metrikler ────────────────────────────────────────────
    [ObservableProperty] private string totalProducts = "0";
    [ObservableProperty] private string todayOrderCount = "0";
    [ObservableProperty] private string todayRevenue = "0 TL";
    [ObservableProperty] private string criticalStockCount = "0";

    // ── Satır 2: Platform metrikleri ──────────────────────────────────────
    [ObservableProperty] private string activePlatformCount = "0";
    [ObservableProperty] private string pendingShipmentCount = "0";
    [ObservableProperty] private string monthlyRevenue = "0 TL";
    [ObservableProperty] private string returnRate = "%0.0";

    [ObservableProperty] private string lastUpdated = "--:--";

    // Zarar uyarı (Chain 10 — PriceLossDetectedEvent)
    [ObservableProperty] private int priceLossCount;
    [ObservableProperty] private string priceLossAlertText = string.Empty;
    public bool HasPriceLossAlerts => PriceLossCount > 0;

    partial void OnPriceLossCountChanged(int value)
    {
        PriceLossAlertText = value > 0
            ? $"{value} urunde satis fiyati alis fiyatinin altinda"
            : string.Empty;
        OnPropertyChanged(nameof(HasPriceLossAlerts));
    }

    // ── Auto-refresh toggle ───────────────────────────────────────────────
    [ObservableProperty] private bool _isAutoRefreshEnabled = true;
    [ObservableProperty] private string _lastRefreshText = "";

    // ── Premium KPI Card ViewModels (6 cards) ─────────────────────────────
    [ObservableProperty] private KpiCardViewModel _todaySalesKpi = new();
    [ObservableProperty] private KpiCardViewModel _weekRevenueKpi = new();
    [ObservableProperty] private KpiCardViewModel _totalProductsKpi = new();
    [ObservableProperty] private KpiCardViewModel _lowStockKpi = new();
    [ObservableProperty] private KpiCardViewModel _pendingOrdersKpi = new();
    [ObservableProperty] private KpiCardViewModel _returnsKpi = new();

    // ── Platform Health (15 platforms) ────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<PlatformStatusViewModel> _platformStatuses = new();

    // ── AI Insight ────────────────────────────────────────────────────────
    [ObservableProperty] private bool _hasAIInsight;
    [ObservableProperty] private string _aiInsightText = string.Empty;

    // ── Critical stock badge count (int for badge binding) ────────────────
    [ObservableProperty] private int _criticalStockBadgeCount;

    // ── LiveCharts2 — Son 7 Gün Platform Satış Grafiği (WPF002) ──────────
    [ObservableProperty] private ISeries[] _salesChartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _salesChartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _salesChartYAxes = Array.Empty<Axis>();

    public ObservableCollection<RecentOrderDto> RecentOrders { get; } = [];
    public ObservableCollection<CriticalStockDto> CriticalStockItems { get; } = [];

    public DashboardAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;

        // Initialize auto-refresh timer (30 second interval)
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += OnRefreshTimerTick;
        _refreshTimer.Start();
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        try
        {
            if (IsAutoRefreshEnabled && !IsLoading)
                await LoadAsync();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Auto-refresh hatasi: {ex.Message}");
#endif
        }
    }

    /// <summary>
    /// Toggle auto-refresh timer on/off when property changes.
    /// </summary>
    partial void OnIsAutoRefreshEnabledChanged(bool value)
    {
        if (value)
            _refreshTimer?.Start();
        else
            _refreshTimer?.Stop();
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
            var summary = await _mediator.Send(
                new GetDashboardSummaryQuery(tenantId), CancellationToken.None);

            // ── Satır 1 ──
            TotalProducts = summary.ActiveProductCount.ToString("N0");
            TodayOrderCount = summary.TodayOrderCount.ToString("N0");
            TodayRevenue = summary.TodaySalesAmount.ToString("N2") + " TL";
            CriticalStockCount = summary.CriticalStockCount.ToString("N0");

            // ── Satır 2 ──
            ActivePlatformCount = summary.ActivePlatformCount.ToString("N0");
            PendingShipmentCount = summary.PendingShipmentCount.ToString("N0");
            MonthlyRevenue = summary.MonthlySalesAmount.ToString("N2") + " TL";
            ReturnRate = "%" + summary.ReturnRate.ToString("F1");

            // ── Premium KPI Cards ──
            TodaySalesKpi = new KpiCardViewModel
            {
                Title = "Bugün Satış",
                Value = $"₺{summary.TodaySalesAmount:N0}",
                ComparisonPeriod = "vs dün"
            };

            WeekRevenueKpi = new KpiCardViewModel
            {
                Title = "Haftalık Gelir",
                Value = $"₺{summary.MonthlySalesAmount:N0}",
                ComparisonPeriod = "vs geçen hafta"
            };

            TotalProductsKpi = new KpiCardViewModel
            {
                Title = "Toplam Ürün",
                Value = summary.ActiveProductCount.ToString("N0"),
                IsPositiveTrend = true,
                ComparisonPeriod = "aktif"
            };

            LowStockKpi = new KpiCardViewModel
            {
                Title = "Kritik Stok",
                Value = summary.CriticalStockCount.ToString("N0"),
                IsPositiveTrend = summary.CriticalStockCount == 0,
                ComparisonPeriod = "ürün"
            };

            PendingOrdersKpi = new KpiCardViewModel
            {
                Title = "Bekleyen Sipariş",
                Value = summary.PendingShipmentCount.ToString("N0"),
                ComparisonPeriod = "kargoya hazır"
            };

            ReturnsKpi = new KpiCardViewModel
            {
                Title = "İade Oranı",
                Value = "%" + summary.ReturnRate.ToString("F1"),
                IsPositiveTrend = summary.ReturnRate < 3.0m,
                ComparisonPeriod = "bu ay"
            };

            // ── Critical stock badge count ──
            CriticalStockBadgeCount = summary.CriticalStockCount;

            // ── Tablolar ──
            RecentOrders.Clear();
            foreach (var o in summary.RecentOrders)
                RecentOrders.Add(new RecentOrderDto
                {
                    OrderNo = o.OrderNumber,
                    Date = o.CreatedAt.ToString("dd.MM.yyyy"),
                    Customer = o.CustomerName,
                    Amount = o.TotalAmount.ToString("N2") + " TL",
                    Status = o.Status,
                    Platform = o.PlatformName ?? "—"
                });

            CriticalStockItems.Clear();
            foreach (var p in summary.CriticalStockItems)
                CriticalStockItems.Add(new CriticalStockDto
                {
                    ProductName = p.ProductName,
                    SKU = p.SKU,
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    Deficit = p.Deficit
                });

            IsEmpty = RecentOrders.Count == 0;

            // ── Grafik verisi — GetSalesChartDataQuery ile gerçek DB'den ──
            await BuildChartDataAsync();

            // ── Refresh timestamp ──
            _lastRefresh = DateTime.Now;
            LastUpdated = _lastRefresh.ToString("HH:mm:ss");
            LastRefreshText = $"Son güncelleme: {_lastRefresh:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dashboard yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void ShowPriceLossDetails()
    {
        // Zarar detay sayfasına navigate — MainWindow sidebar ile entegre
#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[Dashboard] Zarar uyarı detay: {PriceLossCount} ürün");
#endif
    }

    [RelayCommand]
    private void OpenAIInsight()
    {
        // Navigate to AI details or show dialog — placeholder for AI insight navigation
    }

    private static readonly Dictionary<string, string> PlatformColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = "#F27A1A", ["Hepsiburada"] = "#6B21A8", ["Amazon"] = "#FF9900",
        ["N11"] = "#2563EB", ["CicekSepeti"] = "#EC4899", ["eBay"] = "#0064D2",
        ["Shopify"] = "#96BF48", ["WooCommerce"] = "#7F54B3", ["Etsy"] = "#F56400",
        ["OpenCart"] = "#23A1D1", ["Ozon"] = "#005BFF", ["Zalando"] = "#FF6900",
    };

    /// <summary>
    /// LiveCharts2 — GetSalesChartDataQuery ile gerçek DB'den platform bazlı sipariş grafiği.
    /// </summary>
    private async Task BuildChartDataAsync()
    {
        var chartData = await _mediator.Send(new GetSalesChartDataQuery(_tenantProvider.GetCurrentTenantId(), 7));

        var series = new List<ISeries>();
        foreach (var s in chartData.Series)
        {
            var color = PlatformColors.GetValueOrDefault(s.PlatformName, "#6B7280");
            series.Add(new LineSeries<double>
            {
                Name = s.PlatformName,
                Values = s.OrderCountValues.Select(v => (double)v).ToArray(),
                Stroke = new SolidColorPaint(SKColor.Parse(color)) { StrokeThickness = 3 },
                Fill = null,
                GeometrySize = 8
            });
        }

        SalesChartSeries = series.Count > 0
            ? series.ToArray()
            : new ISeries[] { new LineSeries<double> { Name = "Veri yok", Values = Array.Empty<double>() } };

        SalesChartXAxes = new Axis[]
        {
            new Axis
            {
                Labels = chartData.Labels.ToArray(),
                LabelsRotation = 0
            }
        };

        SalesChartYAxes = new Axis[]
        {
            new Axis { Name = "Sipariş" }
        };
    }

    protected override void OnDispose()
    {
        if (_refreshTimer is not null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTimerTick;
            _refreshTimer = null;
        }
    }
}

public class RecentOrderDto
{
    public string OrderNo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}

public class CriticalStockDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public int Deficit { get; set; }
    public string SeverityColor { get; set; } = "#E53935";
    public string StockText { get; set; } = string.Empty;
    public ICommand? OrderCommand { get; set; }
}
