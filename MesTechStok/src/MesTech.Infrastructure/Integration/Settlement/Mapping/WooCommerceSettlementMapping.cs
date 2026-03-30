namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// WooCommerce order export model. Own store — no marketplace commission.
/// Commission = 0, but includes payment gateway fees (Stripe/PayPal/iyzico).
/// </summary>
internal sealed class WooCommerceSettlementLine
{
    public long? OrderId { get; set; }
    public decimal Total { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal RefundTotal { get; set; }
    public decimal GatewayFee { get; set; }
    public string? DateCreated { get; set; }
    public string Currency { get; set; } = "TRY";
}
