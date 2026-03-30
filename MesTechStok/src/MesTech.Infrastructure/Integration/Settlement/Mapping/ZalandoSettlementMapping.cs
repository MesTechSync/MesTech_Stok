namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Zalando ZDirect partner finance report model.
/// Types: SALE, RETURN, COMMISSION, FULFILLMENT, SHIPPING.
/// </summary>
internal sealed class ZalandoSettlementLine
{
    public string Type { get; set; } = "";
    public string? OrderNumber { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal FulfillmentFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal NetAmount { get; set; }
    public string? TransactionDate { get; set; }
    public string Currency { get; set; } = "EUR";
}
