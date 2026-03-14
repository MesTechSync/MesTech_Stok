namespace MesTech.Application.Interfaces;

/// <summary>
/// Dropshipping ürün havuzunu belirli bir platform formatında dışa aktarır.
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A
/// </summary>
public interface IDropshippingExportFormatter
{
    /// <summary>Platform adı. Örn: "Trendyol", "HepsiSeller", "N11", "XML", "CSV", "Excel"</summary>
    string Platform { get; }

    /// <summary>Ürün listesini platforma özgü byte dizisine dönüştürür.</summary>
    Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Dropshipping havuzundan dışa aktarılacak ürün verisi.
/// </summary>
public record PoolProductExportDto(
    string Sku,
    string Name,
    string? Barcode,
    decimal Price,
    int Stock,
    string? Category,
    string? Brand,
    string? ImageUrl,
    string? Description,
    string? SupplierName
);

/// <summary>
/// Dışa aktarma seçenekleri — fiyat markup, gizlilik, stok filtresi.
/// </summary>
public record ExportOptions(
    decimal PriceMarkupPercent = 0,
    bool HideSupplierInfo = true,
    bool IncludeZeroStock = false,
    string Currency = "TRY"
);
