using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Dashboard ViewModel — KPI kartlari + tedarikci performans + karli urunler + oto siparis.
/// </summary>
public partial class DropshipDashboardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


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

    public DropshipDashboardAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var result = await _mediator.Send(new GetDropshipDashboardQuery(_currentUser.TenantId));

            // Original KPIs
            TotalOrders = result.PendingOrders;
            TotalRevenue = result.MonthlyRevenue;
            TotalProfit = result.MonthlyProfit;
            AverageMargin = result.AverageMargin;

            // Enhanced KPIs
            SupplierCount = result.ActiveSuppliers;
            ActiveSupplierText = $"{result.ActiveSuppliers} aktif";
            ActiveProductCount = result.TotalDropshipProducts;
            ProductGrowthText = $"{result.ActiveFeeds} aktif feed";
            ProfitabilityPercent = result.AverageMargin;
            ProfitTrendText = string.Empty;
            AverageDeliveryDays = 0;
            DeliveryTrendText = string.Empty;

            // Supplier performance mapped from TopSuppliers
            Suppliers.Clear();
            foreach (var s in result.TopSuppliers)
            {
                Suppliers.Add(new DropshipSupplierPerformanceDto
                {
                    SupplierName = s.Name,
                    OrderCount = s.OrderCount,
                    CompletedCount = s.OrderCount,
                    ErrorCount = 0,
                    Revenue = s.Revenue,
                    FulfillRate = 0,
                    AvgDeliveryDays = 0,
                    RatingStars = s.AvgMargin.ToString("F1")
                });
            }

            // TopProfitableProducts — no direct mapping in DropshipDashboardDto; clear list
            TopProfitableProducts.Clear();

            IsEmpty = Suppliers.Count == 0;

            // Supplier count from dedicated query
            var suppliers = await _mediator.Send(new GetDropshipSuppliersQuery(_currentUser.TenantId));
            SupplierCount = suppliers.Count;

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
    private Task SaveAutoOrderSettingsAsync()
    {
        IsLoading = true;
        try
        {
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
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
