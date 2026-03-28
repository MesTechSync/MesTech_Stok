using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetProductByBarcode;
using MesTech.Application.Commands.CreateBarcodeScanLog;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// WPF005: Barkod Okuyucu ViewModel — 4 mod (Sayım/Giriş/Çıkış/Transfer),
/// DataGrid ile barkod listesi, kaydedip temizleme desteği.
/// Wired to GetProductByBarcodeQuery + CreateBarcodeScanLogCommand via MediatR.
/// </summary>
public partial class BarcodeReaderViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string scanMode = "Sayım";
    [ObservableProperty] private string barcodeInput = string.Empty;
    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private string statusMessage = string.Empty;

    public ObservableCollection<BarcodeReadItem> ScannedItems { get; } = [];

    public string[] ScanModes { get; } = ["Sayım", "Giriş", "Çıkış", "Transfer"];

    public BarcodeReaderViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

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
            // Resolve product info via MediatR
            var product = await _mediator.Send(new GetProductByBarcodeQuery(barcode), CancellationToken);

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
                var productName = product?.Name ?? $"Barkod Urun ({barcode})";
                var expectedStock = product?.Stock ?? 0;

                ScannedItems.Insert(0, new BarcodeReadItem
                {
                    Barcode = barcode,
                    ProductName = productName,
                    Expected = expectedStock,
                    Counted = 1,
                    Difference = 1 - expectedStock
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
            // Log each scanned barcode via MediatR command
            foreach (var item in ScannedItems)
            {
                await _mediator.Send(new CreateBarcodeScanLogCommand(
                    Barcode: item.Barcode,
                    Format: "EAN13",
                    Source: ScanMode,
                    IsValid: true,
                    RawLength: item.Barcode.Length), CancellationToken);
            }
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
