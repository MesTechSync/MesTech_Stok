using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.ERP.Nebim;

/// <summary>
/// Nebim V3 API — product response model.
/// GET /api/products
/// </summary>
public record NebimProductResponse(
    [property: JsonPropertyName("productCode")] string ProductCode,
    [property: JsonPropertyName("productDescription")] string ProductDescription,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("colorCode")] string? ColorCode,
    [property: JsonPropertyName("itemDimCode1")] string? ItemDimCode1
);

/// <summary>
/// Nebim V3 API — inventory level response model.
/// GET /api/inventory/levels
/// </summary>
public record NebimInventoryResponse(
    [property: JsonPropertyName("productCode")] string ProductCode,
    [property: JsonPropertyName("colorCode")] string? ColorCode,
    [property: JsonPropertyName("itemDimCode1")] string? ItemDimCode1,
    [property: JsonPropertyName("warehouseCode")] string WarehouseCode,
    [property: JsonPropertyName("qty")] int Quantity,
    [property: JsonPropertyName("reservedQty")] int ReservedQuantity,
    [property: JsonPropertyName("availableQty")] int AvailableQuantity
);

/// <summary>
/// Nebim V3 API — invoice response model.
/// POST /api/invoices
/// </summary>
public record NebimInvoiceResponse(
    [property: JsonPropertyName("invoiceNumber")] string InvoiceNumber,
    [property: JsonPropertyName("invoiceDate")] string InvoiceDate,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("status")] string Status
);

/// <summary>
/// Nebim V3 API — customer response model.
/// GET /api/customers
/// </summary>
public record NebimCustomerResponse(
    [property: JsonPropertyName("currAccCode")] string CurrAccCode,
    [property: JsonPropertyName("currAccDescription")] string CurrAccDescription,
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("taxNumber")] string? TaxNumber,
    [property: JsonPropertyName("taxOffice")] string? TaxOffice
);
