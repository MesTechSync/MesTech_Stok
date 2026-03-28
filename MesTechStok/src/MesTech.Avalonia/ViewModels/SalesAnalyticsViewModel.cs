using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// R-02: Satis Analizi ViewModel — wired to GetSalesAnalyticsQuery via MediatR.
/// Platform bazli satis dagilimi + Top 10 urun.
/// </summary>
public partial class SalesAnalyticsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private DateTimeOffset? _dateFrom = new DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    [ObservableProperty] private DateTimeOffset? _dateTo = DateTimeOffset.Now;
    [ObservableProperty] private string? _selectedPlatform = "Tumu";
    [ObservableProperty] private string _totalSalesText = "0.00 TL";
    [ObservableProperty] private string _averageOrderText = "0.00 TL";
    [ObservableProperty] private string _orderCountText = "0";

    public ObservableCollection<string> PlatformOptions { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti", "eBay", "Shopify", "WooCommerce"
    ];

    public ObservableCollection<PlatformSalesItem> PlatformSales { get; } = [];
    public ObservableCollection<TopProductItem> TopProducts { get; } = [];

    public SalesAnalyticsViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Satis Analizi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            PlatformSales.Clear();
            TopProducts.Clear();

            var from = DateFrom?.DateTime ?? new DateTime(DateTime.Now.Year, 1, 1);
            var to = DateTo?.DateTime ?? DateTime.Now;
            var result = await _mediator.Send(
                new GetSalesAnalyticsQuery(_currentUser.TenantId, from, to), ct);

            foreach (var p in result.PlatformBreakdown)
                PlatformSales.Add(new(p.Platform, p.Orders, p.Revenue, p.Percentage));

            foreach (var t in result.TopProducts)
                TopProducts.Add(new(t.ProductName, t.SKU, t.QuantitySold, t.Revenue));

            TotalSalesText = $"{result.TotalRevenue:N2} TL";
            AverageOrderText = $"{result.AverageOrderValue:N2} TL";
            OrderCountText = result.TotalOrders.ToString("N0");
            IsEmpty = PlatformSales.Count == 0;
        }, "Satis verileri yukleniyor");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    /// <summary>FilterCommand — used by "Filtrele" button in filter bar. Alias for Filter.</summary>
    [RelayCommand]
    private async Task Filter() => await LoadAsync();

    [RelayCommand]
    private void ExportExcel()
    {
        System.Diagnostics.Debug.WriteLine("[SalesAnalytics] Excel export tetiklendi");
    }

    [RelayCommand]
    private void ExportPdf()
    {
        System.Diagnostics.Debug.WriteLine("[SalesAnalytics] PDF export tetiklendi");
    }
}

public class PlatformSalesItem
{
    public PlatformSalesItem(string platform, int orderCount, decimal totalAmount, decimal returnRate)
    {
        Platform = platform;
        OrderCount = orderCount;
        TotalAmount = totalAmount;
        ReturnRate = returnRate;
    }

    public string Platform { get; }
    public int OrderCount { get; }
    public decimal TotalAmount { get; }
    public decimal Average => OrderCount > 0 ? TotalAmount / OrderCount : 0m;
    public decimal ReturnRate { get; }

    public string TotalAmountText => TotalAmount.ToString("N2");
    public string AverageText => Average.ToString("N2");
    public string ReturnRateText => $"%{ReturnRate:N1}";
}

public class TopProductItem
{
    public TopProductItem(string productName, string sku, int quantity, decimal amount)
    {
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        Amount = amount;
    }

    public string ProductName { get; }
    public string Sku { get; }
    public int Quantity { get; }
    public decimal Amount { get; }

    public string AmountText => Amount.ToString("N2");
}
