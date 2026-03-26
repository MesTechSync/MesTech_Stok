using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// WPF005: Barkod Okuyucu ViewModel — 4 mod (Sayım/Giriş/Çıkış/Transfer),
/// DataGrid ile barkod listesi, kaydedip temizleme desteği.
/// </summary>
public partial class BarcodeReaderViewModel : ViewModelBase
{
    [ObservableProperty] private string scanMode = "Sayım";
    [ObservableProperty] private string barcodeInput = string.Empty;
    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private string statusMessage = string.Empty;

    public ObservableCollection<BarcodeReadItem> ScannedItems { get; } = [];

    public string[] ScanModes { get; } = ["Sayım", "Giriş", "Çıkış", "Transfer"];

    public override Task LoadAsync()
    {
        IsEmpty = ScannedItems.Count == 0;
        return Task.CompletedTask;
    }

    /// <summary>Enter key on barcode TextBox triggers this command.</summary>
    [RelayCommand]
    private async Task ScanBarcode()
    {
        var barcode = BarcodeInput?.Trim();
        if (string.IsNullOrEmpty(barcode)) return;

        BarcodeInput = string.Empty;
        IsEmpty = false;
        IsLoading = true;

        try
        {
            await Task.Delay(30); // Replace with real MediatR query

            // Check if already in list
            var existing = ScannedItems.FirstOrDefault(x =>
                string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Counted++;
                existing.Difference = existing.Counted - existing.Expected;
            }
            else
            {
                ScannedItems.Insert(0, new BarcodeReadItem
                {
                    Barcode = barcode,
                    ProductName = LookupProductName(barcode),
                    Expected = LookupExpected(barcode),
                    Counted = 1,
                    Difference = 1 - LookupExpected(barcode)
                });
            }

            StatusMessage = $"{ScannedItems.Count} kalem — {ScanMode} modu";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveCount()
    {
        if (ScannedItems.Count == 0) return;
        IsLoading = true;
        try
        {
            await Task.Delay(100); // Replace with real MediatR command
            StatusMessage = $"{ScannedItems.Count} kalem kaydedildi — {ScanMode}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        ScannedItems.Clear();
        StatusMessage = string.Empty;
        IsEmpty = true;
    }

    private static string LookupProductName(string barcode) =>
        barcode.StartsWith("SKU", StringComparison.OrdinalIgnoreCase)
            ? $"Urun ({barcode})"
            : $"Barkod Urun ({barcode})";

    private static int LookupExpected(string barcode) =>
        barcode.Length % 5 == 0 ? 10 : 5; // Placeholder — real query replaces this
}

/// <summary>Single row in the barcode reader count grid.</summary>
public partial class BarcodeReadItem : ObservableObject
{
    [ObservableProperty] private string barcode = string.Empty;
    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private int expected;
    [ObservableProperty] private int counted;
    [ObservableProperty] private int difference;

    public string DifferenceDisplay => Difference == 0 ? "—" : Difference > 0 ? $"+{Difference}" : $"{Difference}";
    public string DifferenceColor => Difference == 0 ? "#388E3C" : Difference > 0 ? "#F57C00" : "#D32F2F";
}
