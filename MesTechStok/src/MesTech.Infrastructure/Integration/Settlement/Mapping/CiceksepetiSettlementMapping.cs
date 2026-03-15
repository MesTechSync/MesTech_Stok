using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Ciceksepeti REST API JSON deserialization models.
/// Endpoint: GET /api/v1/settlements (x-api-key authentication header)
/// 2-week settlement periods.
/// </summary>
internal sealed class CiceksepetiSettlementResponse
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; set; }

    [JsonPropertyName("items")]
    public List<CiceksepetiSettlementItem> Items { get; set; } = new();
}

internal sealed class CiceksepetiSettlementItem
{
    [JsonPropertyName("orderNo")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("saleAmount")]
    public decimal SaleAmount { get; set; }

    [JsonPropertyName("commissionAmount")]
    public decimal CommissionAmount { get; set; }

    [JsonPropertyName("commissionRate")]
    public decimal CommissionRate { get; set; }

    [JsonPropertyName("cargoContribution")]
    public decimal CargoContribution { get; set; }

    [JsonPropertyName("serviceFee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("netAmount")]
    public decimal NetAmount { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}
