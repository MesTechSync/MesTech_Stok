#pragma warning disable CS1998
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Barkod Tarama Modu ViewModel — USB HID wedge uyumlu, tara > aninda sonuc > aksiyon.
/// I-06 Gorev 1: Enter tetiklemeli, &lt; 200ms sonuc hedefi, bulunan/bulunamayan sayaclari.
/// </summary>
public partial class BarcodeScannerViewModel : ViewModelBase
{
    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private string scanInput = string.Empty;
    [ObservableProperty] private int totalScanned;
    [ObservableProperty] private int foundCount;
    [ObservableProperty] private int notFoundCount;
    [ObservableProperty] private long lastResponseTime;

    public ObservableCollection<ScanResultItem> ScanResults { get; } = [];

    public string ScanSummary => $"{TotalScanned} tarama | {FoundCount} bulunan | {NotFoundCount} bulunamayan";

    public BarcodeScannerViewModel()
    {
    }

    public override Task LoadAsync()
    {
        IsEmpty = ScanResults.Count == 0;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Scan()
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
            // Use existing GetProductByBarcodeQuery

            var found = LookupBarcode(barcode);
            stopwatch.Stop();
            LastResponseTime = stopwatch.ElapsedMilliseconds;

            if (found is not null)
            {
                ScanResults.Insert(0, new ScanResultItem
                {
                    Barcode = barcode,
                    Found = true,
                    ProductName = found.Name,
                    Stock = found.Stock,
                    Price = found.Price,
                    WarehouseName = found.Warehouse,
                    MinimumStock = found.MinStock,
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

    /// <summary>Demo barcode lookup — will be replaced by GetProductByBarcodeQuery via MediatR.</summary>
    private static ScanProductData? LookupBarcode(string barcode)
    {
        var products = new Dictionary<string, ScanProductData>(StringComparer.OrdinalIgnoreCase)
        {
            ["8690000123456"] = new("Erkek Tisort Basic", 42, 149.90m, "Ana Depo", 10),
            ["8690000123457"] = new("Kadin Kazak Kis", 8, 249.90m, "Ana Depo", 10),
            ["8681234567890"] = new("Samsung Galaxy S24 Ultra", 45, 54999.99m, "Ana Depo", 10),
            ["8681234567891"] = new("Apple MacBook Air M3", 3, 42999.00m, "Ana Depo", 5),
            ["8681234567892"] = new("Sony WH-1000XM5 Kulaklik", 78, 8499.00m, "Yedek Depo", 20),
            ["SKU-1001"] = new("Samsung Galaxy S24 Ultra", 45, 54999.99m, "Ana Depo", 10),
            ["SKU-1002"] = new("Apple MacBook Air M3", 3, 42999.00m, "Ana Depo", 5),
            ["SKU-1003"] = new("Sony WH-1000XM5 Kulaklik", 78, 8499.00m, "Yedek Depo", 20),
            ["TS-BASIC-001"] = new("Erkek Tisort Basic", 42, 149.90m, "Ana Depo", 10),
        };

        return products.TryGetValue(barcode.Trim(), out var product) ? product : null;
    }

    private record ScanProductData(string Name, int Stock, decimal Price, string Warehouse, int MinStock);
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
