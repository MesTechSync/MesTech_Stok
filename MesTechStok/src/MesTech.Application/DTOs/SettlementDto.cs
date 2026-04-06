namespace MesTech.Application.DTOs;

/// <summary>
/// Platform cari hesap ekstresi.
/// </summary>
public sealed class SettlementDto
{
    public string PlatformCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal TotalSales { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalShippingCost { get; set; }
    public decimal TotalReturnDeduction { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    public List<SettlementLineDto> Lines { get; set; } = new();
}

public sealed class SettlementLineDto
{
    public string? OrderNumber { get; set; }
    public string? TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal CargoDeduction { get; set; }
    public decimal RefundDeduction { get; set; }
    public decimal VatAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime TransactionDate { get; set; }
}

/// <summary>
/// Platform kargo faturasi.
/// </summary>
public sealed class CargoInvoiceDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CargoCompany { get; set; } = string.Empty;
    public int PackageCount { get; set; }
}
