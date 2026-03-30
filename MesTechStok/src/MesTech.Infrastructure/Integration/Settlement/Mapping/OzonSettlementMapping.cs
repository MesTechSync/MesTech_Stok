namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Ozon Finance API v3 /v3/finance/transaction/list response model.
/// Each element in "result.operations" maps to a single settlement line.
/// Reference: https://docs.ozon.ru/api/seller/#operation/FinanceAPI_FinanceTransactionListV3
/// </summary>
internal sealed class OzonSettlementLine
{
    /// <summary>operation_id — unique Ozon operation reference.</summary>
    public long OperationId { get; set; }

    /// <summary>operation_type — e.g. OperationAgentDeliveredToCustomer, OperationItemReturn.</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>operation_type_name — human-readable operation name (Russian).</summary>
    public string OperationTypeName { get; set; } = string.Empty;

    /// <summary>posting.posting_number — Ozon posting/order reference.</summary>
    public string? PostingNumber { get; set; }

    /// <summary>operation_date — ISO 8601 timestamp.</summary>
    public string? OperationDate { get; set; }

    /// <summary>amount — net amount (positive = credit to seller, negative = deduction).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Known Ozon Finance API operation types.
    /// Used for categorizing settlement lines into gross/commission/refund buckets.
    /// </summary>
    public static readonly string[] KnownOperationTypes = new[]
    {
        "OperationAgentDeliveredToCustomer",                 // Sale — maps to gross
        "OperationItemReturn",                               // Return — maps to refundDeduction
        "OperationMarketplaceServiceItemFulfillment",        // FBO fulfillment fee — maps to commission
        "OperationMarketplaceServiceItemDirectFlowTrans",    // Last-mile delivery fee — commission
        "OperationMarketplaceServiceItemReturnFlowTrans",    // Return logistics fee — commission
        "OperationMarketplaceServiceItemDropoffPVZ",         // PVZ delivery fee — commission
        "OperationMarketplaceServiceItemDropoffSC",          // Sorting center fee — commission
        "OperationMarketplaceServiceItemDropoffFF",          // Fulfillment center fee — commission
        "OperationMarketplaceServiceItemDelivToCustomer",    // Customer delivery fee — commission
        "OperationMarketplaceRedistributionOfAcquiringOperation", // Acquiring fee — commission
        "OperationCorrectionSeller",                         // Manual correction
        "OperationMarketplaceWithHoldingForUndeliverableGoods"   // Undeliverable goods hold
    };
}
