using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetProductByBarcode;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Barkod Tarama Modu ViewModel — USB HID wedge uyumlu, tara > aninda sonuc > aksiyon.
/// I-06 Gorev 1: Enter tetiklemeli, &lt; 200ms sonuc hedefi, bulunan/bulunamayan sayaclari.
/// </summary>
public partial class BarcodeScannerViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private string scanInput = string.Empty;
    [ObservableProperty] private int totalScanned;
    [ObservableProperty] private int foundCount;
    [ObservableProperty] private int notFoundCount;
    [ObservableProperty] private long lastResponseTime;

    public ObservableCollection<ScanResultItem> ScanResults { get; } = [];

    public string ScanSummary => $"{TotalScanned} tarama | {FoundCount} bulunan | {NotFoundCount} bulunamayan";

    public BarcodeScannerViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override Task LoadAsync()
    {
        IsEmpty = ScanResults.Count == 0;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        var barcode = ScanInput?.Trim();
        if (string.IsNullOrEmpty(barcode)) return;

        // Clear input immediately — ready for next scan
        ScanInput = string.Empty;
        IsEmpty = false;
        IsLoading = true;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var product = await _mediator.Send(new GetProductByBarcodeQuery(barcode)).ConfigureAwait(false);
            stopwatch.Stop();
            LastResponseTime = stopwatch.ElapsedMilliseconds;

            if (product is not null)
            {
                ScanResults.Insert(0, new ScanResultItem
                {
                    Barcode = barcode,
                    Found = true,
                    ProductName = product.Name,
                    Stock = product.Stock,
                    Price = product.SalePrice,
                    WarehouseName = product.WarehouseName ?? string.Empty,
                    MinimumStock = product.MinimumStock,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                });
                FoundCount++;
            }
            else
            {
                ScanResults.Insert(0, new ScanResultItem
                {
                    Barcode = barcode,
                    Found = false,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                });
                NotFoundCount++;
            }

            TotalScanned++;
            OnPropertyChanged(nameof(ScanSummary));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            HasError = true;
            ErrorMessage = $"Barkod tarama hatasi: {ex.Message}";
            ScanResults.Insert(0, new ScanResultItem
            {
                Barcode = barcode,
                Found = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            });
            NotFoundCount++;
            TotalScanned++;
            OnPropertyChanged(nameof(ScanSummary));
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>LoadDataCommand — used by F5 keybinding and Retry button in error state.</summary>
    [RelayCommand]
    private Task LoadData() => LoadAsync();

    [RelayCommand]
    private void Clear()
    {
        ScanResults.Clear();
        TotalScanned = 0;
        FoundCount = 0;
        NotFoundCount = 0;
        LastResponseTime = 0;
        IsEmpty = true;
        OnPropertyChanged(nameof(ScanSummary));
    }
}

/// <summary>Single scan result item for the timeline list.</summary>
public partial class ScanResultItem : ObservableObject
{
    public string Barcode { get; set; } = string.Empty;
    public bool Found { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int MinimumStock { get; set; }
    public long ResponseTimeMs { get; set; }

    // Computed display properties
    public string StatusIcon => Found ? "\u2713" : "\u2717";
    public string IconBackground => Found ? "#16A34A" : "#EF4444";
    public string BorderColor => Found ? "#E0E6ED" : "#FECACA";
    public string StockText => $"Stok: {Stock}";
    public string PriceText => $"Fiyat: {Price:N2} TL";
    public string ResponseTimeText => $"{ResponseTimeMs} ms";

    public string StockLevelText
    {
        get
        {
            if (Stock <= 0) return "TUKENDI";
            if (Stock <= MinimumStock) return "KRITIK";
            if (Stock <= MinimumStock * 2) return "DUSUK";
            return "YETERLI";
        }
    }

    public string StockLevelColor => StockLevelText switch
    {
        "TUKENDI" => "#D32F2F",
        "KRITIK" => "#D32F2F",
        "DUSUK" => "#F57C00",
        _ => "#388E3C"
    };
}
