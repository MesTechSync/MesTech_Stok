namespace MesTech.Application.DTOs;

/// <summary>
/// Platform'dan cekilen iade/claim bilgisi.
/// </summary>
public sealed class ExternalClaimDto
{
    public string PlatformClaimId { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ReasonDetail { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    public DateTime ClaimDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    public List<ExternalClaimLineDto> Lines { get; set; } = new();
}

public sealed class ExternalClaimLineDto
{
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
