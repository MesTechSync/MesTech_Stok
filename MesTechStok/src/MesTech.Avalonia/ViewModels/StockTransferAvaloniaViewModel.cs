using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.TransferStock;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Features.Stock.Queries.GetStockTransfers;
using MesTech.Application.Queries.GetWarehouses;
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
    private Dictionary<string, Guid> _warehouseIdMap = [];

    public StockTransferAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            TransferStatus = string.Empty;
            var result = await _mediator.Send(new GetStockTransfersQuery(_currentUser.TenantId), ct) ?? [];

            // Depo listesi DB'den
            var warehouses = await _mediator.Send(new GetWarehousesQuery(ActiveOnly: true), ct);
            Warehouses.Clear();
            _warehouseIdMap.Clear();
            foreach (var w in warehouses)
            {
                Warehouses.Add(w.Name);
                _warehouseIdMap[w.Name] = w.Id;
            }

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
        }, "Transfer verileri yuklenirken hata");
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

        _ = LoadSourceProductsAsync();
    }

    private async Task LoadSourceProductsAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetProductsQuery(
                _currentUser.TenantId,
                IsActive: true, PageSize: 200));

            _allSourceProducts = result.Items.Select(p => new TransferProductDto
            {
                ProductId = p.Id,
                Sku = p.SKU ?? string.Empty,
                Ad = p.Name,
                Miktar = p.Stock,
                MinimumStock = p.MinimumStock,
                Depo = SelectedSourceWarehouse ?? "—"
            }).Where(p => p.Miktar > 0).ToList();

            FilterSourceProducts();
        }
        catch (Exception ex)
        {
            TransferStatus = $"Urun listesi yuklenemedi: {ex.Message}";
        }
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
    private async Task TransferAsync()
    {
        if (SelectedSourceWarehouse is null) { TransferStatus = "Kaynak depo secilmedi."; return; }
        if (SelectedTargetWarehouse is null) { TransferStatus = "Hedef depo secilmedi."; return; }
        if (SelectedSourceWarehouse == SelectedTargetWarehouse) { TransferStatus = "Kaynak ve hedef depo ayni olamaz."; return; }
        if (SelectedProduct is null) { TransferStatus = "Urun secilmedi."; return; }
        if (TransferQuantity <= 0) { TransferStatus = "Transfer miktari 0'dan buyuk olmali."; return; }
        if (TransferQuantity > SourceStock) { TransferStatus = "Transfer miktari mevcut stoktan buyuk olamaz."; return; }

        if (!_warehouseIdMap.TryGetValue(SelectedSourceWarehouse, out var sourceId)
            || !_warehouseIdMap.TryGetValue(SelectedTargetWarehouse, out var targetId))
        {
            TransferStatus = "Depo ID cozumlenemedi.";
            return;
        }

        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new TransferStockCommand(
                SelectedProduct.ProductId,
                sourceId,
                targetId,
                TransferQuantity,
                $"UI transfer: {SelectedSourceWarehouse} → {SelectedTargetWarehouse}"), ct);

            if (!result.IsSuccess)
            {
                TransferStatus = $"Hata: {result.ErrorMessage}";
                return;
            }

            TransferHistory.Insert(0, new TransferHistoryDto
            {
                Sku = SelectedProduct.Sku,
                ProductName = SelectedProduct.Ad,
                Source = SelectedSourceWarehouse,
                Target = SelectedTargetWarehouse,
                Quantity = TransferQuantity,
                Date = DateTime.Now
            });

            TransferStatus = $"{SelectedProduct.Ad} — {TransferQuantity} adet transfer edildi. Kalan: {result.SourceRemainingStock}";
            SelectedProduct.Miktar = result.SourceRemainingStock;
            SourceStock = result.SourceRemainingStock;
            RemainingStock = result.SourceRemainingStock;
            TransferQuantity = 0;
        }, "Transfer isleminde hata");
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}

public class TransferProductDto
{
    public Guid ProductId { get; set; }
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
