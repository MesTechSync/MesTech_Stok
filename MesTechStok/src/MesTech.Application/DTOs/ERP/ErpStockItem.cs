namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP stok kalem bilgisi.
/// </summary>
public record ErpStockItem(
    string ProductCode,
    string ProductName,
    int Quantity,
    string UnitCode,
    string? WarehouseCode,
    decimal? UnitCost
);
