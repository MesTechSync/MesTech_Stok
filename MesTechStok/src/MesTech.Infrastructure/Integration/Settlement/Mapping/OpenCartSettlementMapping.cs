using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// OpenCart order data JSON deserialization models.
/// SPECIAL: No platform commission (own store).
/// Data is parsed from order export / simulated DB query results as JSON.
/// Commission = 0, but includes CargoExpense and payment gateway fee.
/// </summary>
internal sealed class OpenCartSettlementResponse
{
    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; set; }

    [JsonPropertyName("orders")]
    public List<OpenCartSettlementItem> Orders { get; set; } = new();
}

internal sealed class OpenCartSettlementItem
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("orderTotal")]
    public decimal OrderTotal { get; set; }

    /// <summary>Payment gateway fee (iyzico, PayTR, etc.) — mapped to ServiceFee.</summary>
    [JsonPropertyName("gatewayFee")]
    public decimal GatewayFee { get; set; }

    /// <summary>Cargo/shipping expense.</summary>
    [JsonPropertyName("cargoExpense")]
    public decimal CargoExpense { get; set; }

    /// <summary>Net payout = OrderTotal - GatewayFee - CargoExpense.</summary>
    [JsonPropertyName("netAmount")]
    public decimal NetAmount { get; set; }

    [JsonPropertyName("orderDate")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }
}
