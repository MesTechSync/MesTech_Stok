namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Amazon SP-API GET_V2_SETTLEMENT_REPORT_DATA_FLAT_FILE tab-separated line model.
/// Each line in the TSV represents a single settlement transaction.
/// </summary>
internal sealed class AmazonSettlementLine
{
    /// <summary>settlement-id column — groups lines into a single settlement batch.</summary>
    public string SettlementId { get; set; } = string.Empty;

    /// <summary>settlement-start-date column (ISO 8601).</summary>
    public string? SettlementStartDate { get; set; }

    /// <summary>settlement-end-date column (ISO 8601).</summary>
    public string? SettlementEndDate { get; set; }

    /// <summary>order-id column — individual order reference.</summary>
    public string? OrderId { get; set; }

    /// <summary>marketplace-name column (e.g. "Amazon.com.tr").</summary>
    public string? MarketplaceName { get; set; }

    /// <summary>amount-type column (ItemPrice, Commission, FBAFees, etc.).</summary>
    public string? AmountType { get; set; }

    /// <summary>amount-description column — detailed fee description.</summary>
    public string? AmountDescription { get; set; }

    /// <summary>amount column — the monetary value (can be negative for deductions).</summary>
    public decimal Amount { get; set; }

    /// <summary>currency column (TRY, USD, EUR).</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>transaction-type column (Order, Refund, etc.).</summary>
    public string? TransactionType { get; set; }

    /// <summary>posted-date column (ISO 8601).</summary>
    public string? PostedDate { get; set; }

    /// <summary>
    /// Known Amazon SP-API TSV column headers for the flat-file settlement report.
    /// </summary>
    public static readonly string[] ExpectedHeaders = new[]
    {
        "settlement-id", "settlement-start-date", "settlement-end-date",
        "deposit-date", "total-amount", "currency",
        "transaction-type", "order-id", "merchant-order-id",
        "adjustment-id", "shipment-id", "marketplace-name",
        "amount-type", "amount-description", "amount",
        "fulfillment-id", "posted-date", "posted-date-time",
        "order-item-code", "merchant-order-item-id",
        "merchant-adjustment-item-id", "sku", "quantity-purchased",
        "promotion-id"
    };
}
