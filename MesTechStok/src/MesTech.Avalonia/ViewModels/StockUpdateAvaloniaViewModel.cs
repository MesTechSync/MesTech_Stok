using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stock Update ViewModel — DataGrid with SKU, Ad, Mevcut Stok, Yeni Stok, Platform.
/// 10 demo items + Bulk update button. M1 Avalonia canlandirma — Beta Agent.
/// </summary>
public partial class StockUpdateAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string updateStatus = string.Empty;

    public ObservableCollection<StockUpdateItemDto> StockItems { get; } = [];

    private List<StockUpdateItemDto> _allItems = [];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        UpdateStatus = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load

            _allItems =
            [
                new() { Sku = "TRY-ELK-001", UrunAdi = "Samsung Galaxy S24 Ultra", MevcutStok = 45, YeniStok = 45, Platform = "Trendyol" },
                new() { Sku = "HB-BLG-002", UrunAdi = "Apple MacBook Air M3", MevcutStok = 12, YeniStok = 12, Platform = "Hepsiburada" },
                new() { Sku = "N11-AKS-003", UrunAdi = "Sony WH-1000XM5 Kulaklik", MevcutStok = 78, YeniStok = 78, Platform = "N11" },
                new() { Sku = "TRY-AKS-004", UrunAdi = "Logitech MX Master 3S Mouse", MevcutStok = 156, YeniStok = 156, Platform = "Trendyol" },
                new() { Sku = "CS-MNT-005", UrunAdi = "Dell U2723QE 4K Monitor", MevcutStok = 8, YeniStok = 15, Platform = "Ciceksepeti" },
                new() { Sku = "AMZ-GYM-006", UrunAdi = "Dyson V15 Detect Supurge", MevcutStok = 23, YeniStok = 23, Platform = "Amazon" },
                new() { Sku = "OC-EV-007", UrunAdi = "Philips Airfryer XXL", MevcutStok = 0, YeniStok = 50, Platform = "OpenCart" },
                new() { Sku = "TRY-GYM-008", UrunAdi = "Karaca Hatir Turk Kahve Makinesi", MevcutStok = 340, YeniStok = 340, Platform = "Trendyol" },
                new() { Sku = "HB-KSA-009", UrunAdi = "Vestel 55 inc 4K Smart TV", MevcutStok = 5, YeniStok = 20, Platform = "Hepsiburada" },
                new() { Sku = "N11-SPR-010", UrunAdi = "Nike Air Max 270 Ayakkabi", MevcutStok = 67, YeniStok = 67, Platform = "N11" },
            ];

            ApplyFilters();
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

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(s =>
                s.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.UrunAdi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Platform.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        StockItems.Clear();
        foreach (var item in filtered)
            StockItems.Add(item);

        TotalCount = StockItems.Count;
        IsEmpty = StockItems.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task BulkUpdateAsync()
    {
        IsLoading = true;
        UpdateStatus = string.Empty;
        try
        {
            await Task.Delay(800); // Simulate bulk update

            int updatedCount = 0;
            foreach (var item in _allItems)
            {
                if (item.MevcutStok != item.YeniStok)
                {
                    item.MevcutStok = item.YeniStok;
                    updatedCount++;
                }
            }

            ApplyFilters();
            UpdateStatus = updatedCount > 0
                ? $"{updatedCount} urun stoku guncellendi."
                : "Guncellenecek stok degisikligi bulunamadi.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Toplu guncelleme basarisiz: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allItems.Count > 0)
            ApplyFilters();
    }
}

public class StockUpdateItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string UrunAdi { get; set; } = string.Empty;
    public int MevcutStok { get; set; }
    public int YeniStok { get; set; }
    public string Platform { get; set; } = string.Empty;
}
