using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

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

    public StockLotAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
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
            await Task.Delay(200); // Will be replaced with MediatR query

            Suppliers.Clear();
            Suppliers.Add("ABC Elektronik A.S.");
            Suppliers.Add("XYZ Teknoloji Ltd.");
            Suppliers.Add("Global Parts Inc.");
            Suppliers.Add("Mega Tedarik");

            Warehouses.Clear();
            Warehouses.Add("Ana Depo");
            Warehouses.Add("Yedek Depo");
            Warehouses.Add("Iade Depo");

            RecentLots.Clear();
            RecentLots.Add(new LotEntryDto { LotNo = "LOT-2026-001", ProductName = "Samsung Galaxy S24", Quantity = 50, UnitCost = 28500m, Supplier = "ABC Elektronik A.S.", Warehouse = "Ana Depo", ExpiryDate = null, CreatedAt = DateTime.Now.AddDays(-3) });
            RecentLots.Add(new LotEntryDto { LotNo = "LOT-2026-002", ProductName = "Anker PowerCore 20000", Quantity = 200, UnitCost = 450m, Supplier = "Global Parts Inc.", Warehouse = "Ana Depo", ExpiryDate = DateTime.Now.AddMonths(18), CreatedAt = DateTime.Now.AddDays(-1) });
            RecentLots.Add(new LotEntryDto { LotNo = "LOT-2026-003", ProductName = "Sony WH-1000XM5 Kulaklik", Quantity = 30, UnitCost = 6200m, Supplier = "XYZ Teknoloji Ltd.", Warehouse = "Yedek Depo", ExpiryDate = null, CreatedAt = DateTime.Now });

            IsEmpty = false;
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
            await Task.Delay(500); // Will be replaced with MediatR command

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
