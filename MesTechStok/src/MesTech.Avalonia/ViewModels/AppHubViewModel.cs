using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// G098: App Hub — login sonrası karşılama hub'ı.
/// Sol: son siparişler + düşük stok + bekleyen fatura
/// Orta: günlük özet KPI
/// Sağ: servis durumu
/// Alt: hızlı aksiyon çubuğu
/// </summary>
public partial class AppHubViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AppHubViewModel> _logger;

    // === Günlük Özet KPI ===
    [ObservableProperty] private int todayOrderCount;
    [ObservableProperty] private decimal todayRevenue;
    [ObservableProperty] private int newCustomerCount;
    [ObservableProperty] private int pendingShipmentCount;
    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string todayDate = string.Empty;

    // === Sol Panel: Hızlı Bakış ===
    public ObservableCollection<RecentOrderItem> RecentOrders { get; } = [];
    public ObservableCollection<LowStockItem> LowStockAlerts { get; } = [];
    public ObservableCollection<PendingInvoiceItem> PendingInvoices { get; } = [];

    // === Sağ Panel: Servis Durumu ===
    public ObservableCollection<ServiceStatusItem> ServiceStatuses { get; } = [];

    public AppHubViewModel(IMediator mediator, ILogger<AppHubViewModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var now = DateTime.Now;
            Greeting = now.Hour switch
            {
                < 12 => "Gunaydin",
                < 18 => "Iyi gunler",
                _ => "Iyi aksamlar"
            };
            TodayDate = now.ToString("dd MMMM yyyy, dddd", new System.Globalization.CultureInfo("tr-TR"));

            await LoadDashboardSummaryAsync();
            await LoadRecentOrdersAsync();
            await LoadLowStockAsync();
            await LoadPendingInvoicesAsync();
            await LoadServiceStatusAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Hub yuklenemedi: {ex.Message}";
            _logger.LogError(ex, "AppHub load failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDashboardSummaryAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetDashboardSummaryQuery(Guid.Empty));
            if (result is not null)
            {
                TodayOrderCount = result.TodayOrderCount;
                TodayRevenue = result.TodaySalesAmount;
                NewCustomerCount = result.ActivePlatformCount; // proxy: aktif platform sayısı
                PendingShipmentCount = result.PendingShipmentCount;
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard summary query failed, using defaults");
        }
        // Fallback: safe defaults
        TodayOrderCount = 0;
        TodayRevenue = 0;
        NewCustomerCount = 0;
        PendingShipmentCount = 0;
    }

    private Task LoadRecentOrdersAsync()
    {
        RecentOrders.Clear();
        RecentOrders.Add(new("SP-2024-001", "Trendyol", "Beklemede", 1249.90m));
        RecentOrders.Add(new("SP-2024-002", "Hepsiburada", "Kargoda", 899.00m));
        RecentOrders.Add(new("SP-2024-003", "N11", "Tamamlandi", 2150.00m));
        RecentOrders.Add(new("SP-2024-004", "Ciceksepeti", "Hazirlaniyor", 450.50m));
        RecentOrders.Add(new("SP-2024-005", "Amazon", "Onaylandi", 3200.00m));
        return Task.CompletedTask;
    }

    private Task LoadLowStockAsync()
    {
        LowStockAlerts.Clear();
        LowStockAlerts.Add(new("Apple iPhone 15 Pro", 2, 10));
        LowStockAlerts.Add(new("Samsung Galaxy S24", 5, 15));
        LowStockAlerts.Add(new("Sony WH-1000XM5", 1, 5));
        return Task.CompletedTask;
    }

    private Task LoadPendingInvoicesAsync()
    {
        PendingInvoices.Clear();
        PendingInvoices.Add(new("FTR-2024-041", "Trendyol Ocak Hakedis", 45_200.00m));
        PendingInvoices.Add(new("FTR-2024-042", "HB Subat Hakedis", 28_750.00m));
        return Task.CompletedTask;
    }

    private Task LoadServiceStatusAsync()
    {
        ServiceStatuses.Clear();
        ServiceStatuses.Add(new("PostgreSQL", true, "3ms"));
        ServiceStatuses.Add(new("Redis", true, "1ms"));
        ServiceStatuses.Add(new("RabbitMQ", true, "2ms"));
        ServiceStatuses.Add(new("Trendyol API", true, "142ms"));
        ServiceStatuses.Add(new("Hepsiburada API", true, "238ms"));
        ServiceStatuses.Add(new("N11 API", false, "timeout"));
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

// === DTO Records ===
public record RecentOrderItem(string OrderNumber, string Platform, string Status, decimal Amount);
public record LowStockItem(string ProductName, int CurrentStock, int MinStock);
public record PendingInvoiceItem(string InvoiceNumber, string Description, decimal Amount);
public record ServiceStatusItem(string Name, bool IsHealthy, string ResponseTime);
