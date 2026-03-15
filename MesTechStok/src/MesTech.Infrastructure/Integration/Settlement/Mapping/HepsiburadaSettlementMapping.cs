using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Hepsiburada Merchant API /settlements JSON deserialization models.
/// Endpoint: GET /settlements?startDate={}&amp;endDate={}
/// </summary>
internal sealed class HepsiburadaSettlementResponse
{
    [JsonPropertyName("data")]
    public HepsiburadaSettlementData? Data { get; set; }
}

internal sealed class HepsiburadaSettlementData
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("settlements")]
    public List<HepsiburadaSettlementItem> Settlements { get; set; } = new();

    [JsonPropertyName("summary")]
    public HepsiburadaSettlementSummary? Summary { get; set; }
}

internal sealed class HepsiburadaSettlementItem
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

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

    [JsonPropertyName("netAmount")]
    public decimal NetAmount { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

internal sealed class HepsiburadaSettlementSummary
{
    [JsonPropertyName("totalSaleAmount")]
    public decimal TotalSaleAmount { get; set; }

    [JsonPropertyName("totalCommissionAmount")]
    public decimal TotalCommissionAmount { get; set; }

    [JsonPropertyName("totalCargoContribution")]
    public decimal TotalCargoContribution { get; set; }

    [JsonPropertyName("totalNetAmount")]
    public decimal TotalNetAmount { get; set; }
}
