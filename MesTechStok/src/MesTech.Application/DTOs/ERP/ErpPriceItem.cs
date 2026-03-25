namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP fiyat kalem bilgisi — ERP'den cekilecek urun fiyat verisi.
/// </summary>
public record ErpPriceItem(
    string ProductCode,
    string ProductName,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal? ListPrice,
    string CurrencyCode
);
