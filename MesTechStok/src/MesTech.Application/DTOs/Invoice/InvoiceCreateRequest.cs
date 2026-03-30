using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

/// <summary>
/// Invoice Create Request data transfer object.
/// </summary>
public record InvoiceCreateRequest
{
    public Guid OrderId { get; init; }
    public PlatformType Platform { get; init; }
    public string PlatformOrderId { get; init; } = string.Empty;
    public InvoiceType Type { get; init; }

    public InvoiceCustomerInfo Customer { get; init; } = new InvoiceCustomerInfo(string.Empty, null, null, string.Empty, null, null);

    public IReadOnlyList<InvoiceCreateLine> Lines { get; init; } = [];
    public decimal TotalAmount { get; init; }
    public KdvRate DefaultKdv { get; init; }
    public string? IstisnaSebebi { get; init; }
    public InvoiceCargoInfo? Cargo { get; init; }
    public string? PaymentAgent { get; init; }
    public string? Note { get; init; }
}

public record InvoiceCustomerInfo(
    string Name,
    string? TaxNumber,
    string? TaxOffice,
    string Address,
    string? Email,
    string? Phone);

public record InvoiceCreateLine(
    string ProductName,
    string? SKU,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal? DiscountAmount);
