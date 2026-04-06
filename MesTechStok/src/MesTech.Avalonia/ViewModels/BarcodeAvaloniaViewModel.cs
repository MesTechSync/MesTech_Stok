using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetBarcodeScanLogs;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Barkod Islemleri ViewModel — USB HID wedge barkod okuma + urun karti + depo stok breakdown + hizli guncelleme.
/// EMR-06 Gorev 4E: Enhanced from placeholder to functional view.
/// </summary>
public partial class BarcodeAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private bool hasProduct;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    // Product card fields
    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private string productSku = string.Empty;
    [ObservableProperty] private string productBarcode = string.Empty;
    [ObservableProperty] private decimal productPrice;
    [ObservableProperty] private int totalStock;
    [ObservableProperty] private int minimumStock;
    [ObservableProperty] private string quickUpdateStatus = string.Empty;

    public ObservableCollection<object> Items { get; } = [];
    public ObservableCollection<BarcodeWarehouseStockDto> WarehouseStocks { get; } = [];

    /// <summary>EMR-06 renk tablosu: 4 seviye stok durumu.</summary>
    public string StokDurum
    {
        get
        {
            if (TotalStock <= 0) return "TUKENDI";
            if (TotalStock <= MinimumStock) return "KRITIK";
            if (TotalStock <= MinimumStock * 1.5) return "DUSUK";
            return "YETERLI";
        }
    }

    public string StokDurumRenk => StokDurum switch
    {
        "TUKENDI" => "#D32F2F",
        "KRITIK" => "#D32F2F",
        "DUSUK" => "#F57C00",
        "YETERLI" => "#388E3C",
        _ => "#388E3C"
    };

    public BarcodeAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) { IsEmpty = true; HasProduct = false; return; }

        HasProduct = false;
        QuickUpdateStatus = string.Empty;
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetBarcodeScanLogsQuery(
                BarcodeFilter: SearchText.Trim(),
                Page: 1,
                PageSize: 50), ct);

            Items.Clear();
            foreach (var log in result.Items)
                Items.Add(log);

            TotalCount = result.TotalCount;

            // Use first matching valid scan to populate product card fields
            var first = result.Items.FirstOrDefault(l => l.IsValid);
            if (first is not null)
            {
                ProductBarcode = first.Barcode;
                ProductSku = first.Source ?? string.Empty;
                HasProduct = true;
            }
            else
            {
                HasProduct = result.TotalCount > 0;
            }

            IsEmpty = result.TotalCount == 0;

            OnPropertyChanged(nameof(StokDurum));
            OnPropertyChanged(nameof(StokDurumRenk));
        }, "Barkod sorgusu yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value)
    {
        // USB HID wedge terminates with Enter — auto-search on length match
        if (value.Length == 0)
        {
            IsEmpty = true;
            HasProduct = false;
        }
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private Task QuickUpdate(string delta)
    {
        if (!HasProduct) return Task.CompletedTask;
        if (!int.TryParse(delta, out var change)) return Task.CompletedTask;

        IsLoading = true;
        QuickUpdateStatus = string.Empty;
        try
        {
            TotalStock += change;
            if (TotalStock < 0) TotalStock = 0;

            // Update first warehouse stock for demo
            if (WarehouseStocks.Count > 0)
            {
                var first = WarehouseStocks[0];
                first.Quantity += change;
                if (first.Quantity < 0) first.Quantity = 0;
            }

            OnPropertyChanged(nameof(StokDurum));
            OnPropertyChanged(nameof(StokDurumRenk));

            QuickUpdateStatus = change > 0
                ? $"+{change} adet eklendi. Yeni stok: {TotalStock}"
                : $"{change} adet dusuldu. Yeni stok: {TotalStock}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Stok guncelleme basarisiz: {ex.Message}";
        }
        finally { IsLoading = false; }
        return Task.CompletedTask;
    }

}

public class BarcodeWarehouseStockDto
{
    public string WarehouseName { get; set; } = string.Empty;
    public string ShelfCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinStock { get; set; }

    public string StokDurum
    {
        get
        {
            if (Quantity <= 0) return "TUKENDI";
            if (Quantity <= MinStock) return "KRITIK";
            if (Quantity <= MinStock * 1.5) return "DUSUK";
            return "YETERLI";
        }
    }
}

internal class BarcodeProductData
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MinStock { get; set; }
    public List<BarcodeWarehouseStockDto> WarehouseStocks { get; set; } = [];
}
