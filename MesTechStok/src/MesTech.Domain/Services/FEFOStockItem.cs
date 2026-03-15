namespace MesTech.Domain.Services;

/// <summary>
/// FEFO siralama icin stok kalemi girdi modeli.
/// </summary>
public record FEFOStockItem(
    Guid ProductId,
    string SKU,
    DateTime? ExpirationDate,
    decimal Quantity,
    string Location,
    string? LotNumber = null);
