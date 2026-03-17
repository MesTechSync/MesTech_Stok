using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Stock Update (Toplu Stok Guncelleme) screen.
/// Displays stock items with editable "Yeni Stok" column and bulk update action.
/// Will be wired to BulkUpdateStockCommand via MediatR when full migration starts.
/// </summary>
public partial class StockMovementAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int changedCount;
    [ObservableProperty] private string updateStatus = string.Empty;

    public ObservableCollection<StockMovementItemDto> Items { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        UpdateStatus = string.Empty;
        try
        {
            await Task.Delay(80); // Simulate async load

            Items.Clear();
            Items.Add(new StockMovementItemDto { Sku = "TRD-SAM-001", UrunAdi = "Samsung Galaxy S24 Ultra", MevcutStok = 45, YeniStok = 45, Platform = "Trendyol" });
            Items.Add(new StockMovementItemDto { Sku = "HB-APL-002", UrunAdi = "Apple iPhone 15 Pro", MevcutStok = 22, YeniStok = 22, Platform = "Hepsiburada" });
            Items.Add(new StockMovementItemDto { Sku = "N11-SON-003", UrunAdi = "Sony WH-1000XM5 Kulaklik", MevcutStok = 78, YeniStok = 78, Platform = "N11" });
            Items.Add(new StockMovementItemDto { Sku = "CS-LOG-004", UrunAdi = "Logitech MX Master 3S Mouse", MevcutStok = 156, YeniStok = 156, Platform = "CicekSepeti" });
            Items.Add(new StockMovementItemDto { Sku = "AMZ-DEL-005", UrunAdi = "Dell U2723QE 4K Monitor", MevcutStok = 8, YeniStok = 8, Platform = "Amazon" });
            Items.Add(new StockMovementItemDto { Sku = "TRD-XIA-006", UrunAdi = "Xiaomi Redmi Note 13 Pro", MevcutStok = 210, YeniStok = 210, Platform = "Trendyol" });
            Items.Add(new StockMovementItemDto { Sku = "SHP-HPE-007", UrunAdi = "HP EliteBook 840 G10", MevcutStok = 14, YeniStok = 14, Platform = "Shopify" });
            Items.Add(new StockMovementItemDto { Sku = "WOO-LEN-008", UrunAdi = "Lenovo ThinkPad X1 Carbon", MevcutStok = 31, YeniStok = 31, Platform = "WooCommerce" });
            Items.Add(new StockMovementItemDto { Sku = "PZR-ASU-009", UrunAdi = "Asus ROG Strix G16 Laptop", MevcutStok = 6, YeniStok = 6, Platform = "Pazarama" });
            Items.Add(new StockMovementItemDto { Sku = "PTT-ANK-010", UrunAdi = "Anker PowerCore 26800mAh", MevcutStok = 340, YeniStok = 340, Platform = "PttAVM" });

            // Subscribe to changes
            foreach (var item in Items)
                item.PropertyChanged += (_, _) => RecalculateChangedCount();

            TotalCount = Items.Count;
            IsEmpty = Items.Count == 0;
            RecalculateChangedCount();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Stok verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RecalculateChangedCount()
    {
        ChangedCount = Items.Count(x => x.MevcutStok != x.YeniStok);
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task BulkUpdate()
    {
        var changed = Items.Where(x => x.MevcutStok != x.YeniStok).ToList();
        if (changed.Count == 0)
        {
            UpdateStatus = "Degisiklik yapilmadi.";
            return;
        }

        IsLoading = true;
        UpdateStatus = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate bulk update

            foreach (var item in changed)
                item.MevcutStok = item.YeniStok;

            RecalculateChangedCount();
            UpdateStatus = $"{changed.Count} urun stok bilgisi guncellendi.";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Guncelleme hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class StockMovementItemDto : ObservableObject
{
    public string Sku { get; set; } = string.Empty;
    public string UrunAdi { get; set; } = string.Empty;
    [ObservableProperty] private int mevcutStok;
    [ObservableProperty] private int yeniStok;
    public string Platform { get; set; } = string.Empty;
}
