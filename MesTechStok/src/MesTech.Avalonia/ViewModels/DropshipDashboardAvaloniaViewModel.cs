using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Dashboard ViewModel — KPI kartlari + tedarikci performans + karli urunler + oto siparis.
/// TODO: Replace demo data with MediatR.Send(new GetDropshipDashboardQuery()) when A1 CQRS is ready.
/// </summary>
public partial class DropshipDashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI values (original)
    [ObservableProperty] private int totalOrders;
    [ObservableProperty] private decimal totalRevenue;
    [ObservableProperty] private decimal totalProfit;
    [ObservableProperty] private decimal averageMargin;

    // Enhanced KPI values
    [ObservableProperty] private int supplierCount;
    [ObservableProperty] private string activeSupplierText = string.Empty;
    [ObservableProperty] private int activeProductCount;
    [ObservableProperty] private string productGrowthText = string.Empty;
    [ObservableProperty] private decimal profitabilityPercent;
    [ObservableProperty] private string profitTrendText = string.Empty;
    [ObservableProperty] private double averageDeliveryDays;
    [ObservableProperty] private string deliveryTrendText = string.Empty;

    // Auto-order settings
    [ObservableProperty] private bool isAutoOrderEnabled;
    [ObservableProperty] private decimal autoOrderThreshold = 5;

    public ObservableCollection<DropshipSupplierPerformanceDto> Suppliers { get; } = [];
    public ObservableCollection<DropshipProfitableProductDto> TopProfitableProducts { get; } = [];

    public DropshipDashboardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // TODO: Replace with MediatR.Send(new GetDropshipDashboardQuery()) when A1 CQRS is ready
            await Task.Delay(300);

            // Original KPIs
            TotalOrders = 347;
            TotalRevenue = 284_500.00m;
            TotalProfit = 42_675.00m;
            AverageMargin = 15.0m;

            // Enhanced KPIs
            SupplierCount = 12;
            ActiveSupplierText = "8 aktif";
            ActiveProductCount = 1_284;
            ProductGrowthText = "+42 bu hafta";
            ProfitabilityPercent = 15.0m;
            ProfitTrendText = "+2.3% onceki aya gore";
            AverageDeliveryDays = 2.8;
            DeliveryTrendText = "-0.3 gun iyilesme";

            // Supplier performance (enhanced with new columns)
            Suppliers.Clear();
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "ABC Elektronik", OrderCount = 145, CompletedCount = 141, ErrorCount = 4, Revenue = 128_400m, FulfillRate = 97.2, AvgDeliveryDays = 2.1, RatingStars = "4.8" });
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "XYZ Bilisim", OrderCount = 98, CompletedCount = 93, ErrorCount = 5, Revenue = 87_300m, FulfillRate = 94.8, AvgDeliveryDays = 2.8, RatingStars = "4.5" });
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "Guney Aksesuar", OrderCount = 67, CompletedCount = 61, ErrorCount = 6, Revenue = 42_100m, FulfillRate = 91.5, AvgDeliveryDays = 3.2, RatingStars = "4.2" });
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "Delta Depo", OrderCount = 37, CompletedCount = 33, ErrorCount = 4, Revenue = 26_700m, FulfillRate = 88.9, AvgDeliveryDays = 3.5, RatingStars = "3.9" });

            // Top 10 profitable products
            TopProfitableProducts.Clear();
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "Samsung Galaxy S24 Ultra", SalesCount = 42, Profit = 8_820m, MarginPercent = 21.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "iPhone 15 Pro Max", SalesCount = 28, Profit = 7_560m, MarginPercent = 18.5 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "MacBook Air M3", SalesCount = 15, Profit = 5_250m, MarginPercent = 12.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "AirPods Pro 2", SalesCount = 67, Profit = 4_690m, MarginPercent = 28.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "Samsung Galaxy Tab S9", SalesCount = 22, Profit = 3_960m, MarginPercent = 16.5 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "Xiaomi 14 Ultra", SalesCount = 31, Profit = 3_410m, MarginPercent = 14.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "JBL Flip 6", SalesCount = 54, Profit = 2_700m, MarginPercent = 32.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "Logitech MX Master 3S", SalesCount = 38, Profit = 2_280m, MarginPercent = 24.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "Sony WH-1000XM5", SalesCount = 19, Profit = 2_090m, MarginPercent = 22.0 });
            TopProfitableProducts.Add(new DropshipProfitableProductDto { ProductName = "Apple Watch SE", SalesCount = 24, Profit = 1_920m, MarginPercent = 16.0 });

            IsEmpty = Suppliers.Count == 0;

            // Auto-order defaults
            IsAutoOrderEnabled = true;
            AutoOrderThreshold = 5;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dropshipping verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAutoOrderSettingsAsync()
    {
        // TODO: Replace with MediatR.Send(new SaveAutoOrderSettingsCommand()) when ready
        IsLoading = true;
        try
        {
            await Task.Delay(300);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class DropshipSupplierPerformanceDto
{
    public string SupplierName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int CompletedCount { get; set; }
    public int ErrorCount { get; set; }
    public decimal Revenue { get; set; }
    public double FulfillRate { get; set; }
    public double AvgDeliveryDays { get; set; }
    public string RatingStars { get; set; } = string.Empty;
}

public class DropshipProfitableProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public decimal Profit { get; set; }
    public double MarginPercent { get; set; }
}
