namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Shopify Payouts API transaction model.
/// Types: charge, refund, dispute, reserve, adjustment, payout.
/// </summary>
internal sealed class ShopifySettlementLine
{
    public long Id { get; set; }
    public string Type { get; set; } = "";
    public string? SourceOrderId { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal Net { get; set; }
    public string? ProcessedAt { get; set; }
    public string Currency { get; set; } = "TRY";
}
