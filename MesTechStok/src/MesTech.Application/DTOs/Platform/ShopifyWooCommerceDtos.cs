namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Shipment info DTO for platform-level shipment notifications.
/// Used by Shopify (fulfillments) and WooCommerce (order status + tracking meta).
/// </summary>
public sealed class ShipmentInfoDto
{
    /// <summary>Cargo tracking number.</summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>Cargo company name (e.g. "Yurtici Kargo", "UPS").</summary>
    public string TrackingCompany { get; set; } = string.Empty;

    /// <summary>Cargo tracking URL for customer.</summary>
    public string? TrackingUrl { get; set; }

    /// <summary>Whether to notify the customer about shipment.</summary>
    public bool NotifyCustomer { get; set; } = true;
}

/// <summary>
/// Shopify inventory location DTO.
/// Maps from GET /admin/api/2024-01/locations.json response.
/// </summary>
public sealed class InventoryLocationDto
{
    /// <summary>Shopify location ID (numeric string).</summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>Location name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Location address line.</summary>
    public string? Address { get; set; }

    /// <summary>Whether the location is active.</summary>
    public bool Active { get; set; }
}

/// <summary>
/// Product variant DTO — used by both Shopify and WooCommerce.
/// Shopify: variants, WooCommerce: variations.
/// </summary>
public sealed class ProductVariantDto
{
    /// <summary>Platform variant/variation ID.</summary>
    public string VariantId { get; set; } = string.Empty;

    /// <summary>Variant SKU.</summary>
    public string? Sku { get; set; }

    /// <summary>Variant title or option label.</summary>
    public string? Title { get; set; }

    /// <summary>Variant price (string for decimal precision).</summary>
    public string? Price { get; set; }

    /// <summary>Current stock quantity.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Whether stock is managed for this variant.</summary>
    public bool ManageStock { get; set; }
}

/// <summary>
/// Single product update item for WooCommerce batch update.
/// </summary>
public sealed class BatchProductUpdateDto
{
    /// <summary>WooCommerce product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>New price (null = no change).</summary>
    public decimal? Price { get; set; }

    /// <summary>New stock quantity (null = no change).</summary>
    public int? Stock { get; set; }
}

/// <summary>
/// Result of a WooCommerce batch product update.
/// </summary>
public sealed class BatchUpdateResultDto
{
    /// <summary>Number of successfully updated products.</summary>
    public int Updated { get; set; }

    /// <summary>List of errors encountered during batch update.</summary>
    public List<string> Errors { get; set; } = new();
}
