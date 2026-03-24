namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// eBay Finances API v1 /sell/finances/v1/transaction JSON response model.
/// Each element in the "transactions" array maps to a single settlement line.
/// Reference: https://developer.ebay.com/api-docs/sell/finances/resources/transaction/methods/getTransactions
/// </summary>
internal sealed class EbaySettlementLine
{
    /// <summary>transactionId — unique eBay transaction reference.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>transactionType — SALE, REFUND, CREDIT, SHIPPING_LABEL, TRANSFER, etc.</summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>orderId — eBay order reference (may be null for non-order transactions).</summary>
    public string? OrderId { get; set; }

    /// <summary>transactionDate — ISO 8601 timestamp.</summary>
    public string? TransactionDate { get; set; }

    /// <summary>amount.value — net amount the seller receives (can be negative for deductions).</summary>
    public decimal Amount { get; set; }

    /// <summary>amount.currency — 3-letter currency code (USD, EUR, GBP, TRY).</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>totalFeeBasisAmount.value — gross amount before eBay fees.</summary>
    public decimal TotalFeeBasisAmount { get; set; }

    /// <summary>totalFeeAmount.value — total eBay fees (final value fee + regulatory fee etc.).</summary>
    public decimal TotalFeeAmount { get; set; }

    /// <summary>
    /// Known eBay Finances API transaction types.
    /// Used for categorizing settlement lines into gross/commission/refund buckets.
    /// </summary>
    public static readonly string[] KnownTransactionTypes = new[]
    {
        "SALE",              // Completed sale — maps to gross
        "REFUND",            // Buyer refund — maps to refundDeduction
        "CREDIT",            // eBay credit (promotion, correction)
        "SHIPPING_LABEL",    // Purchased shipping label — maps to cargoDeduction
        "TRANSFER",          // Payout to seller's bank
        "NON_SALE_CHARGE",   // Subscription, store fees
        "DISPUTE",           // Buyer dispute/chargeback
        "ADJUSTMENT"         // Manual adjustment
    };
}
