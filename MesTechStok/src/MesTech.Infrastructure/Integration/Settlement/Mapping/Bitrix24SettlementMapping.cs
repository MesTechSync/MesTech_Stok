using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Bitrix24 CRM deal/order settlement JSON models.
/// Bitrix24 is a CRM+shop platform — settlement data comes from deal exports.
/// Commission = platform subscription fee allocation per deal (configurable %).
/// </summary>
internal sealed class Bitrix24SettlementResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("periodStart")]
    public string? PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public string? PeriodEnd { get; set; }

    [JsonPropertyName("deals")]
    public List<Bitrix24SettlementItem> Deals { get; set; } = new();
}

internal sealed class Bitrix24SettlementItem
{
    [JsonPropertyName("dealId")]
    public string? DealId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("opportunity")]
    public decimal Opportunity { get; set; }

    [JsonPropertyName("commissionAmount")]
    public decimal CommissionAmount { get; set; }

    [JsonPropertyName("cargoAmount")]
    public decimal CargoAmount { get; set; }

    [JsonPropertyName("netAmount")]
    public decimal NetAmount { get; set; }

    [JsonPropertyName("closeDate")]
    public string? CloseDate { get; set; }

    [JsonPropertyName("currencyId")]
    public string? CurrencyId { get; set; }
}
