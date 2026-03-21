namespace MesTech.Application.DTOs;

/// <summary>
/// Product Export data transfer object.
/// </summary>
public sealed record ProductExportDto(
    string Sku,
    string Name,
    decimal Price,
    int Stock,
    string? Category,
    string? Barcode);

public sealed record StockExportDto(
    string Sku,
    string Name,
    int Stock);

public sealed record PriceExportDto(
    string Sku,
    string Name,
    decimal Price);

public sealed record OrderExportDto(
    string OrderNumber,
    string CustomerName,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    string? TrackingNumber);

public sealed record ProfitabilityExportDto(
    string Sku,
    string Name,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);
