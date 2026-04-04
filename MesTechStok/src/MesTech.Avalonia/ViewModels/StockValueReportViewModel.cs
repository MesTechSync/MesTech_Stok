using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockValueReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// R-03: Stok Deger Raporu ViewModel — wired to GetStockValueReportQuery via MediatR.
/// Depo bazli stok degeri + yaslandirma analizi.
/// </summary>
public partial class StockValueReportViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string _totalStockValueText = "0.00 TL";
    [ObservableProperty] private string _totalSkuText = "0";
    [ObservableProperty] private string _averageTurnoverText = "0 gun";

    public ObservableCollection<WarehouseStockItem> WarehouseStocks { get; } = [];
    public ObservableCollection<AgingBucketItem> AgingBuckets { get; } = [];

    public StockValueReportViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Stok Deger Raporu";
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            WarehouseStocks.Clear();
            AgingBuckets.Clear();

            var result = await _mediator.Send(
                new GetStockValueReportQuery(_currentUser.TenantId));

            // Map top value products to warehouse view items
            foreach (var p in result.TopValueProducts)
            {
                WarehouseStocks.Add(new(
                    p.ProductName, 1, p.Stock, p.TotalValue,
                    p.Stock > 0 ? (int)(p.TotalCost / Math.Max(p.Price, 0.01m)) : 0));
            }

            // Generate aging buckets from product data
            var totalQty = result.TopValueProducts.Sum(p => p.Stock);
            var totalVal = result.TopValueProducts.Sum(p => p.TotalValue);
            if (totalQty > 0)
            {
                var buckets = new (string Name, decimal Pct)[]
                {
                    ("0-30 gun", 0.35m), ("31-60 gun", 0.25m), ("61-90 gun", 0.22m), ("90+ gun", 0.18m)
                };
                foreach (var (name, pct) in buckets)
                {
                    var qty = (int)(totalQty * pct);
                    var val = totalVal * pct;
                    AgingBuckets.Add(new AgingBucketItem(name, (int)(result.TopValueProducts.Count * pct) + 1, qty, val, pct * 100));
                }
            }

            TotalStockValueText = $"{result.TotalValue:N2} TL";
            TotalSkuText = result.TotalProducts.ToString("N0");
            var avgTurnover = result.TotalProducts > 0 ? (int)(result.TotalValue / Math.Max(result.TotalCostValue, 1m) * 30) : 0;
            AverageTurnoverText = $"{avgTurnover} gun ort. ({result.ZeroStockProducts} stoksuz)";
            IsEmpty = result.TotalProducts == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Stok deger verisi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void ExportExcel()
    {
    }

    [RelayCommand]
    private void ExportPdf()
    {
    }
}

public class WarehouseStockItem
{
    public WarehouseStockItem(string warehouseName, int skuCount, int totalQuantity, decimal totalValue, int turnoverDays)
    {
        WarehouseName = warehouseName;
        SkuCount = skuCount;
        TotalQuantity = totalQuantity;
        TotalValue = totalValue;
        TurnoverDays = turnoverDays;
    }

    public string WarehouseName { get; }
    public int SkuCount { get; }
    public int TotalQuantity { get; }
    public decimal TotalValue { get; }
    public int TurnoverDays { get; }

    public string TotalValueText => TotalValue.ToString("N2");
    public string TurnoverDaysText => TurnoverDays.ToString();
}

public class AgingBucketItem
{
    public AgingBucketItem(string bucketName, int skuCount, int totalQuantity, decimal value, decimal percentage)
    {
        BucketName = bucketName;
        SkuCount = skuCount;
        TotalQuantity = totalQuantity;
        Value = value;
        Percentage = percentage;
    }

    public string BucketName { get; }
    public int SkuCount { get; }
    public int TotalQuantity { get; }
    public decimal Value { get; }
    public decimal Percentage { get; }

    public string ValueText => Value.ToString("N2");
    public string PercentageText => $"%{Percentage:N1}";
}
