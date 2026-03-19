using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia Dashboard — 8 KPI cards + son siparişler + kritik stok DataGrid.
/// Demo data'dan gerçek MediatR GetDashboardSummaryQuery'ye dönüştürüldü (EMR-03-v2 ALAN-D).
/// </summary>
public partial class DashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

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

    // L/E/E states
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<RecentOrderDto> RecentOrders { get; } = [];
    public ObservableCollection<CriticalStockDto> CriticalStockItems { get; } = [];

    public DashboardAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public async Task LoadAsync()
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
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
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
}
