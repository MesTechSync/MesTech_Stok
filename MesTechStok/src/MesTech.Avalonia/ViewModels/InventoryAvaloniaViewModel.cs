using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Envanter yonetimi ViewModel — stok alarm gostergeli DataGrid.
/// Will be wired to GetInventorySummaryQuery via MediatR when full migration starts.
/// </summary>
public partial class InventoryAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int alarmCount;

    public ObservableCollection<InventoryItemDto> Items { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300);

            Items.Clear();
            Items.Add(new InventoryItemDto { Sku = "SKU-1001", Ad = "Samsung Galaxy S24", Miktar = 45, MinStok = 10, MaxStok = 200, Depo = "Ana Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1002", Ad = "Apple MacBook Air M3", Miktar = 3, MinStok = 5, MaxStok = 50, Depo = "Ana Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1003", Ad = "Sony WH-1000XM5 Kulaklik", Miktar = 78, MinStok = 20, MaxStok = 300, Depo = "Yedek Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1004", Ad = "Logitech MX Master 3S", Miktar = 2, MinStok = 15, MaxStok = 150, Depo = "Ana Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1005", Ad = "Dell U2723QE Monitor", Miktar = 8, MinStok = 5, MaxStok = 40, Depo = "Yedek Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1006", Ad = "Anker PowerCore 20000", Miktar = 120, MinStok = 30, MaxStok = 500, Depo = "Ana Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1007", Ad = "Xiaomi Mi Band 8", Miktar = 0, MinStok = 25, MaxStok = 400, Depo = "Yedek Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1008", Ad = "HP LaserJet Pro M404", Miktar = 15, MinStok = 5, MaxStok = 30, Depo = "Ana Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1009", Ad = "Canon EOS R50 Kamera", Miktar = 4, MinStok = 8, MaxStok = 25, Depo = "Yedek Depo" });
            Items.Add(new InventoryItemDto { Sku = "SKU-1010", Ad = "JBL Charge 5 Hoparlor", Miktar = 56, MinStok = 10, MaxStok = 200, Depo = "Ana Depo" });

            TotalCount = Items.Count;
            AlarmCount = Items.Count(i => i.Miktar < i.MinStok);
            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Envanter yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class InventoryItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int Miktar { get; set; }
    public int MinStok { get; set; }
    public int MaxStok { get; set; }
    public string Depo { get; set; } = string.Empty;
    public bool IsAlarm => Miktar < MinStok;
}
