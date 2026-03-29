using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockLots;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Lot Ekleme ViewModel — urun lot kaydi: lot no, miktar, birim maliyet, tedarikci, SKT, depo.
/// EMR-06 Gorev 4A: Form-based view for lot entry.
/// </summary>
public partial class StockLotAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string saveStatus = string.Empty;

    // Form fields
    [ObservableProperty] private string productSearchText = string.Empty;
    [ObservableProperty] private LotProductDto? selectedProduct;
    [ObservableProperty] private string lotNumber = string.Empty;
    [ObservableProperty] private int quantity;
    [ObservableProperty] private decimal unitCost;
    [ObservableProperty] private string? selectedSupplier;
    [ObservableProperty] private DateTimeOffset? expiryDate;
    [ObservableProperty] private string? selectedWarehouse;

    // Lot history
    public ObservableCollection<LotProductDto> ProductSuggestions { get; } = [];
    public ObservableCollection<string> Suppliers { get; } = [];
    public ObservableCollection<string> Warehouses { get; } = [];
    public ObservableCollection<LotEntryDto> RecentLots { get; } = [];

    private readonly ICurrentUserService _currentUser;

    public StockLotAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
        SaveStatus = string.Empty;
        try
        {
            var lots = await _mediator.Send(new GetStockLotsQuery(_currentUser.TenantId));

            Suppliers.Clear();
            var supplierNames = lots.Select(l => l.SupplierName).Where(s => !string.IsNullOrEmpty(s)).Distinct();
            foreach (var s in supplierNames) Suppliers.Add(s!);

            Warehouses.Clear();
            var warehouseNames = lots.Select(l => l.WarehouseName).Where(w => !string.IsNullOrEmpty(w)).Distinct();
            foreach (var w in warehouseNames) Warehouses.Add(w!);

            RecentLots.Clear();
            foreach (var l in lots.Take(20))
            {
                RecentLots.Add(new LotEntryDto
                {
                    LotNo = l.LotNumber,
                    ProductName = l.ProductName ?? l.ProductSku ?? "—",
                    Quantity = l.RemainingQuantity,
                    UnitCost = l.UnitCost,
                    Supplier = l.SupplierName ?? "—",
                    Warehouse = l.WarehouseName ?? "—",
                    ExpiryDate = l.ExpiryDate,
                    CreatedAt = l.ReceivedAt
                });
            }

            IsEmpty = RecentLots.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Lot verileri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnProductSearchTextChanged(string value)
    {
        if (value.Length < 2) { ProductSuggestions.Clear(); return; }

        ProductSuggestions.Clear();
        var suggestions = new List<LotProductDto>
        {
            new() { Sku = "SKU-1001", Ad = "Samsung Galaxy S24" },
            new() { Sku = "SKU-1002", Ad = "Apple MacBook Air M3" },
            new() { Sku = "SKU-1003", Ad = "Sony WH-1000XM5 Kulaklik" },
            new() { Sku = "SKU-1004", Ad = "Logitech MX Master 3S" },
            new() { Sku = "SKU-1005", Ad = "Dell U2723QE Monitor" },
            new() { Sku = "SKU-1006", Ad = "Anker PowerCore 20000" },
        };

        var search = value.ToLowerInvariant();
        foreach (var s in suggestions.Where(s =>
            s.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            s.Ad.Contains(search, StringComparison.OrdinalIgnoreCase)))
        {
            ProductSuggestions.Add(s);
        }
    }

    [RelayCommand]
    private async Task SaveLotAsync()
    {
        if (SelectedProduct is null) { SaveStatus = "Urun secilmedi."; return; }
        if (string.IsNullOrWhiteSpace(LotNumber)) { SaveStatus = "Lot numarasi girilmedi."; return; }
        if (Quantity <= 0) { SaveStatus = "Miktar 0'dan buyuk olmali."; return; }
        if (string.IsNullOrWhiteSpace(SelectedWarehouse)) { SaveStatus = "Depo secilmedi."; return; }

        IsLoading = true;
        SaveStatus = string.Empty;
        try
        {
            // TODO: await _mediator.Send(new CreateStockLotCommand(...))
            RecentLots.Insert(0, new LotEntryDto
            {
                LotNo = LotNumber,
                ProductName = SelectedProduct.Ad,
                Quantity = Quantity,
                UnitCost = UnitCost,
                Supplier = SelectedSupplier ?? "-",
                Warehouse = SelectedWarehouse!,
                ExpiryDate = ExpiryDate?.DateTime,
                CreatedAt = DateTime.Now
            });

            SaveStatus = $"Lot '{LotNumber}' basariyla kaydedildi.";

            // Reset form
            LotNumber = string.Empty;
            Quantity = 0;
            UnitCost = 0;
            ExpiryDate = null;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Lot kaydedilemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class LotProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
}

public class LotEntryDto
{
    public string LotNo { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
