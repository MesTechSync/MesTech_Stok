using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// R-03: Stok Deger Raporu ViewModel.
/// Depo bazli stok degeri + yaslandirma analizi.
/// </summary>
public partial class StockValueReportViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string _totalStockValueText = "0.00 TL";
    [ObservableProperty] private string _totalSkuText = "0";
    [ObservableProperty] private string _averageTurnoverText = "0 gun";

    public ObservableCollection<WarehouseStockItem> WarehouseStocks { get; } = [];
    public ObservableCollection<AgingBucketItem> AgingBuckets { get; } = [];

    public StockValueReportViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "Stok Deger Raporu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            WarehouseStocks.Clear();
            AgingBuckets.Clear();

            // TODO: Replace with IMediator query — await _mediator.Send(new GetStockValueReportQuery(), CancellationToken);
            await Task.Delay(300, CancellationToken);

            WarehouseStocks.Add(new("Ana Depo - Istanbul", 842, 15_430, 2_456_780.50m, 28));
            WarehouseStocks.Add(new("Amazon FBA", 312, 8_720, 1_245_600.00m, 18));
            WarehouseStocks.Add(new("Hepsilojistik", 198, 4_560, 678_340.25m, 22));
            WarehouseStocks.Add(new("Yedek Depo - Ankara", 156, 3_210, 412_890.75m, 45));

            var totalValue = WarehouseStocks.Sum(w => w.TotalValue);
            var totalSku = WarehouseStocks.Sum(w => w.SkuCount);
            var avgTurnover = WarehouseStocks.Count > 0
                ? (int)WarehouseStocks.Average(w => w.TurnoverDays)
                : 0;

            // Aging analysis
            AgingBuckets.Add(new("0-30 gun", 524, 18_200, 2_890_450.00m, 58.7m));
            AgingBuckets.Add(new("31-60 gun", 287, 7_640, 1_234_560.00m, 25.1m));
            AgingBuckets.Add(new("61-90 gun", 98, 3_120, 512_340.00m, 10.4m));
            AgingBuckets.Add(new("90+ gun", 45, 1_960, 286_261.50m, 5.8m));

            TotalStockValueText = $"{totalValue:N2} TL";
            TotalSkuText = totalSku.ToString("N0");
            AverageTurnoverText = $"{avgTurnover} gun";
            IsEmpty = WarehouseStocks.Count == 0;
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
