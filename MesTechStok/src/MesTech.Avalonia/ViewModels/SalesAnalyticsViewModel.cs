using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// R-02: Satis Analizi ViewModel.
/// Platform bazli satis dagilimi + Top 10 urun.
/// </summary>
public partial class SalesAnalyticsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private DateTimeOffset? _dateFrom = new DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    [ObservableProperty] private DateTimeOffset? _dateTo = DateTimeOffset.Now;
    [ObservableProperty] private string? _selectedPlatform = "Tumu";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _totalSalesText = "0.00 TL";
    [ObservableProperty] private string _averageOrderText = "0.00 TL";
    [ObservableProperty] private string _orderCountText = "0";

    public ObservableCollection<string> PlatformOptions { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti", "eBay", "Shopify", "WooCommerce"
    ];

    public ObservableCollection<PlatformSalesItem> PlatformSales { get; } = [];
    public ObservableCollection<TopProductItem> TopProducts { get; } = [];

    public SalesAnalyticsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Satis Analizi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            PlatformSales.Clear();
            TopProducts.Clear();

            // TODO: Replace with IMediator query — await _mediator.Send(new GetSalesAnalyticsQuery(...), CancellationToken);
            await Task.Delay(300, CancellationToken);

            PlatformSales.Add(new("Trendyol", 1247, 456_890.50m, 2.1m));
            PlatformSales.Add(new("Hepsiburada", 834, 312_450.00m, 1.8m));
            PlatformSales.Add(new("N11", 423, 178_320.75m, 3.2m));
            PlatformSales.Add(new("Amazon", 612, 289_100.25m, 1.5m));
            PlatformSales.Add(new("Ciceksepeti", 298, 124_780.00m, 2.7m));

            TopProducts.Add(new("Kablosuz Bluetooth Kulaklik", "SKU-001", 342, 68_400.00m));
            TopProducts.Add(new("USB-C Hub Adaptoru", "SKU-002", 287, 43_050.00m));
            TopProducts.Add(new("Mekanik Klavye RGB", "SKU-003", 198, 59_400.00m));
            TopProducts.Add(new("Ergonomik Mouse", "SKU-004", 176, 26_400.00m));
            TopProducts.Add(new("Laptop Standı Aluminyum", "SKU-005", 165, 33_000.00m));
            TopProducts.Add(new("Webcam 1080p", "SKU-006", 154, 23_100.00m));
            TopProducts.Add(new("Monitor Kolu", "SKU-007", 143, 35_750.00m));
            TopProducts.Add(new("Kablo Duzenleyici Set", "SKU-008", 132, 6_600.00m));
            TopProducts.Add(new("Mousepad XL", "SKU-009", 121, 9_680.00m));
            TopProducts.Add(new("USB Mikrofon", "SKU-010", 118, 29_500.00m));

            var totalSales = PlatformSales.Sum(p => p.TotalAmount);
            var orderCount = PlatformSales.Sum(p => p.OrderCount);
            var avgOrder = orderCount > 0 ? totalSales / orderCount : 0m;

            TotalSalesText = $"{totalSales:N2} TL";
            AverageOrderText = $"{avgOrder:N2} TL";
            OrderCountText = orderCount.ToString("N0");
            IsEmpty = PlatformSales.Count == 0;
        }, "Satis verileri yukleniyor");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task FilterAsync() => await LoadAsync();

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
