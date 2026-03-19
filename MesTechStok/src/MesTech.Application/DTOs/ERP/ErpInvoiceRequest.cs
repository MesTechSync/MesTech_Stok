namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP fatura olusturma istegi.
/// </summary>
public record ErpInvoiceRequest(
    string CustomerCode,
    string CustomerName,
    string? TaxId,
    List<ErpInvoiceLineRequest> Lines,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Currency = "TRY",
    string? Notes = null
);

/// <summary>
/// ERP fatura satiri.
/// </summary>
public record ErpInvoiceLineRequest(
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    int TaxRate,
    decimal? DiscountAmount
);
