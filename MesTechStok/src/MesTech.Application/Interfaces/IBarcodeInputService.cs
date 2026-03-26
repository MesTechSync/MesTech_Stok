using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

/// <summary>
/// USB HID barkod okuyucu input servisi.
/// 4 mod destekler: StockCount, ProductSearch, OrderPicking, LabelPrint.
/// Tarama sonucu GetProductByBarcodeQuery ile urun bulunur,
/// BarcodeScanLog ile kayit altina alinir.
/// </summary>
public interface IBarcodeInputService
{
    /// <summary>Barkod tarandiginda moda gore islemi baslatir.</summary>
    Task<BarcodeInputResult> ProcessScanAsync(
        Guid tenantId,
        string barcode,
        BarcodeScanMode mode,
        string? deviceId = null,
        CancellationToken ct = default);
}

public enum BarcodeScanMode
{
    /// <summary>Stok sayim — barkod tarandiginda sayim listesine +1 eklenir</summary>
    StockCount = 1,

    /// <summary>Urun arama — barkod tarandiginda urun detay sayfasi acilir</summary>
    ProductSearch = 2,

    /// <summary>Siparis hazirlama — barkod tarandiginda siparisteki urunler tiklenir</summary>
    OrderPicking = 3,

    /// <summary>Etiket basimi — barkod tarandiginda urun etiketi yazdirilir</summary>
    LabelPrint = 4
}

public sealed class BarcodeInputResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public ProductDto? Product { get; init; }
    public BarcodeScanMode Mode { get; init; }
    public string Barcode { get; init; } = string.Empty;

    public static BarcodeInputResult Success(ProductDto product, BarcodeScanMode mode, string barcode)
        => new() { IsSuccess = true, Product = product, Mode = mode, Barcode = barcode };

    public static BarcodeInputResult NotFound(string barcode, BarcodeScanMode mode)
        => new() { IsSuccess = false, ErrorMessage = $"Urun bulunamadi: {barcode}", Mode = mode, Barcode = barcode };
}
