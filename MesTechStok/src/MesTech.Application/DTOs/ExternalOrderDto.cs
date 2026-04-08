namespace MesTech.Application.DTOs;

/// <summary>
/// Platform'dan cekilen siparis bilgisi.
/// </summary>
public sealed class ExternalOrderDto
{
    public string PlatformOrderId { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Musteri
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerTaxNumber { get; set; }

    // Fatura adresi (invoiceAddress) — e-fatura kesimi icin gerekli
    public string? InvoiceAddress { get; set; }
    public string? InvoiceCity { get; set; }
    public string? InvoiceDistrict { get; set; }
    public string? InvoiceFullName { get; set; }

    // Tutar
    public decimal TotalAmount { get; set; }
    public decimal? GrossAmount { get; set; }
    public decimal? TotalDiscount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? ShippingCost { get; set; }
    public string Currency { get; set; } = "TRY";

    // Kargo
    public string? ShipmentPackageId { get; set; }
    public string? CargoProviderName { get; set; }
    public string? CargoTrackingNumber { get; set; }

    // Fatura
    public string? InvoiceLink { get; set; }

    // Tarihler
    public DateTime OrderDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    // Kalemler
    public List<ExternalOrderLineDto> Lines { get; set; } = new();
}

public sealed class ExternalOrderLineDto
{
    public string? PlatformLineId { get; set; }
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}
