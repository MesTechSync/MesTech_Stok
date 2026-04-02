using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockTransfers;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Depolar Arasi Transfer ViewModel — kaynak depo → hedef depo urun transferi.
/// EMR-06 Gorev 4A: Kaynak/hedef depo secici + urun secici + transfer miktari.
/// </summary>
public partial class StockTransferAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string transferStatus = string.Empty;

    [ObservableProperty] private string? selectedSourceWarehouse;
    [ObservableProperty] private string? selectedTargetWarehouse;
    [ObservableProperty] private string productSearchText = string.Empty;
    [ObservableProperty] private TransferProductDto? selectedProduct;
    [ObservableProperty] private int transferQuantity;
    [ObservableProperty] private int sourceStock;
    [ObservableProperty] private int remainingStock;

    public ObservableCollection<string> Warehouses { get; } = [];
    public ObservableCollection<TransferProductDto> SourceProducts { get; } = [];
    public ObservableCollection<TransferHistoryDto> TransferHistory { get; } = [];

    private List<TransferProductDto> _allSourceProducts = [];

    public StockTransferAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
        TransferStatus = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetStockTransfersQuery(_currentUser.TenantId)) ?? [];

            Warehouses.Clear();
            Warehouses.Add("Ana Depo");
            Warehouses.Add("Yedek Depo");
            Warehouses.Add("Iade Depo");

            TransferHistory.Clear();
            foreach (var item in result)
            {
                TransferHistory.Add(new TransferHistoryDto
                {
                    Sku = item.SKU,
                    ProductName = item.ProductName,
                    Source = item.MovementType,
                    Target = item.Reference ?? string.Empty,
                    Quantity = item.Quantity,
                    Date = item.MovementDate
                });
            }

            IsEmpty = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Transfer verileri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnSelectedSourceWarehouseChanged(string? value)
    {
        LoadSourceProducts();
        SelectedProduct = null;
        SourceStock = 0;
        RemainingStock = 0;
        TransferQuantity = 0;
    }

    partial void OnSelectedProductChanged(TransferProductDto? value)
    {
        if (value is not null)
        {
            SourceStock = value.Miktar;
            RemainingStock = value.Miktar - TransferQuantity;
        }
        else
        {
            SourceStock = 0;
            RemainingStock = 0;
        }
    }

    partial void OnTransferQuantityChanged(int value)
    {
        RemainingStock = SourceStock - value;
    }

    partial void OnProductSearchTextChanged(string value)
    {
        FilterSourceProducts();
    }

    private void LoadSourceProducts()
    {
        _allSourceProducts.Clear();
        SourceProducts.Clear();

        if (SelectedSourceWarehouse is null) return;

        // Will be replaced with MediatR query by warehouse
        var allItems = new List<TransferProductDto>
        {
            new() { Sku = "SKU-1001", Ad = "Samsung Galaxy S24", Miktar = 45, MinimumStock = 10, Depo = "Ana Depo" },
            new() { Sku = "SKU-1002", Ad = "Apple MacBook Air M3", Miktar = 3, MinimumStock = 5, Depo = "Ana Depo" },
            new() { Sku = "SKU-1004", Ad = "Logitech MX Master 3S", Miktar = 156, MinimumStock = 15, Depo = "Ana Depo" },
            new() { Sku = "SKU-1006", Ad = "Anker PowerCore 20000", Miktar = 120, MinimumStock = 30, Depo = "Ana Depo" },
            new() { Sku = "SKU-1003", Ad = "Sony WH-1000XM5 Kulaklik", Miktar = 78, MinimumStock = 20, Depo = "Yedek Depo" },
            new() { Sku = "SKU-1005", Ad = "Dell U2723QE Monitor", Miktar = 8, MinimumStock = 5, Depo = "Yedek Depo" },
            new() { Sku = "SKU-1007", Ad = "Xiaomi Mi Band 8", Miktar = 12, MinimumStock = 25, Depo = "Yedek Depo" },
            new() { Sku = "SKU-1008", Ad = "HP LaserJet Pro M404", Miktar = 15, MinimumStock = 5, Depo = "Iade Depo" },
            new() { Sku = "SKU-1010", Ad = "JBL Charge 5 Hoparlor", Miktar = 56, MinimumStock = 10, Depo = "Iade Depo" },
        };

        _allSourceProducts = allItems.Where(i => i.Depo == SelectedSourceWarehouse).ToList();
        FilterSourceProducts();
    }

    private void FilterSourceProducts()
    {
        var filtered = _allSourceProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(ProductSearchText) && ProductSearchText.Length >= 2)
        {
            var search = ProductSearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Ad.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        SourceProducts.Clear();
        foreach (var item in filtered)
            SourceProducts.Add(item);
    }

    [RelayCommand]
    private Task TransferAsync()
    {
        if (SelectedSourceWarehouse is null) { TransferStatus = "Kaynak depo secilmedi."; return Task.CompletedTask; }
        if (SelectedTargetWarehouse is null) { TransferStatus = "Hedef depo secilmedi."; return Task.CompletedTask; }
        if (SelectedSourceWarehouse == SelectedTargetWarehouse) { TransferStatus = "Kaynak ve hedef depo ayni olamaz."; return Task.CompletedTask; }
        if (SelectedProduct is null) { TransferStatus = "Urun secilmedi."; return Task.CompletedTask; }
        if (TransferQuantity <= 0) { TransferStatus = "Transfer miktari 0'dan buyuk olmali."; return Task.CompletedTask; }
        if (TransferQuantity > SourceStock) { TransferStatus = "Transfer miktari mevcut stoktan buyuk olamaz."; return Task.CompletedTask; }

        IsLoading = true;
        TransferStatus = string.Empty;
        try
        {
            TransferHistory.Insert(0, new TransferHistoryDto
            {
                Sku = SelectedProduct.Sku,
                ProductName = SelectedProduct.Ad,
                Source = SelectedSourceWarehouse,
                Target = SelectedTargetWarehouse,
                Quantity = TransferQuantity,
                Date = DateTime.Now
            });

            TransferStatus = $"{SelectedProduct.Ad} — {TransferQuantity} adet {SelectedSourceWarehouse} → {SelectedTargetWarehouse} transfer edildi.";

            // Update local state
            SelectedProduct.Miktar -= TransferQuantity;
            SourceStock = SelectedProduct.Miktar;
            RemainingStock = SelectedProduct.Miktar;
            TransferQuantity = 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Transfer basarisiz: {ex.Message}";
        }
        finally { IsLoading = false; }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}

public class TransferProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int Miktar { get; set; }
    public int MinimumStock { get; set; }
    public string Depo { get; set; } = string.Empty;

    /// <summary>EMR-06 renk tablosu: 4 seviye stok durumu.</summary>
    public string StokDurum
    {
        get
        {
            if (Miktar <= 0) return "TUKENDI";
            if (Miktar <= MinimumStock) return "KRITIK";
            if (Miktar <= MinimumStock * 1.5) return "DUSUK";
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
}

public class TransferHistoryDto
{
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
}
