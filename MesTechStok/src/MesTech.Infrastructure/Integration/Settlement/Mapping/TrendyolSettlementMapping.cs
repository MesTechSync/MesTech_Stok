using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Trendyol Finance API JSON deserialization models.
/// Endpoint: GET /suppliers/{supplierId}/finance/settlements
/// </summary>
internal sealed class TrendyolSettlementResponse
{
    [JsonPropertyName("totalElements")]
    public int TotalElements { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("content")]
    public List<TrendyolSettlementItem> Content { get; set; } = new();
}

internal sealed class TrendyolSettlementItem
{
    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("grossSalesAmount")]
    public decimal GrossSalesAmount { get; set; }

    [JsonPropertyName("commissionAmount")]
    public decimal CommissionAmount { get; set; }

    [JsonPropertyName("serviceFee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("cargoDeduction")]
    public decimal CargoDeduction { get; set; }

    [JsonPropertyName("refundDeduction")]
    public decimal RefundDeduction { get; set; }

    [JsonPropertyName("netAmount")]
    public decimal NetAmount { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("commissionRate")]
    public decimal CommissionRate { get; set; }
}
