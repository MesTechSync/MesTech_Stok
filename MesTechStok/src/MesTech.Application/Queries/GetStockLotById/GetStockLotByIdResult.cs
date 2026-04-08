namespace MesTech.Application.Queries.GetStockLotById;

/// <summary>
/// StockLot GetById query result DTO.
/// </summary>
public sealed record GetStockLotByIdResult(
    Guid Id,
    Guid TenantId,
    Guid ProductId,
    string LotNumber,
    int Quantity,
    int RemainingQuantity,
    decimal UnitCost,
    decimal TotalCost,
    Guid? SupplierId,
    string? SupplierName,
    Guid? WarehouseId,
    string? WarehouseName,
    DateTime? ExpiryDate,
    DateTime ReceivedAt,
    string? Notes,
    DateTime CreatedAt);
