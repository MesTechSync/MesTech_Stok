using System.Collections.ObjectModel;
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
        await SafeExecuteAsync(async ct =>
        {
            WarehouseStocks.Clear();
            AgingBuckets.Clear();

            var result = await _mediator.Send(
                new GetStockValueReportQuery(_currentUser.TenantId), ct);

            // Map top value products to warehouse view items
            foreach (var p in result.TopValueProducts)
            {
                WarehouseStocks.Add(new(
                    p.ProductName, 1, p.Stock, p.TotalValue,
                    p.Stock > 0 ? (int)(p.TotalCost / Math.Max(p.Price, 0.01m)) : 0));
            }

            TotalStockValueText = $"{result.TotalValue:N2} TL";
            TotalSkuText = result.TotalProducts.ToString("N0");
            AverageTurnoverText = $"{result.ZeroStockProducts} stoksuz";
            IsEmpty = result.TotalProducts == 0;
        }, "Stok deger verisi yukleniyor");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void ExportExcel()
    {
        System.Diagnostics.Debug.WriteLine("[StockValueReport] Excel export tetiklendi");
    }

    [RelayCommand]
    private void ExportPdf()
    {
        System.Diagnostics.Debug.WriteLine("[StockValueReport] PDF export tetiklendi");
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
