using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Pazarama REST API JSON deserialization models.
/// Endpoint: GET /settlements (OAuth2 token authentication)
/// </summary>
internal sealed class PazaramaSettlementResponse
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; set; }

    [JsonPropertyName("settlements")]
    public List<PazaramaSettlementItem> Settlements { get; set; } = new();
}

internal sealed class PazaramaSettlementItem
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("commission")]
    public decimal Commission { get; set; }

    [JsonPropertyName("commissionRate")]
    public decimal CommissionRate { get; set; }

    [JsonPropertyName("cargoFee")]
    public decimal CargoFee { get; set; }

    [JsonPropertyName("netPayout")]
    public decimal NetPayout { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}
