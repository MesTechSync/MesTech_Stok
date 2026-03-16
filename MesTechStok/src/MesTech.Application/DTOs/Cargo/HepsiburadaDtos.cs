namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Hepsiburada API response modelleri — adapter icinde kullanilir.
/// </summary>
public class HbListingsResponse
{
    public List<HbListing> Listings { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Hepsiburada listing modeli.
/// </summary>
public class HbListing
{
    public string HepsiburadaSku { get; set; } = string.Empty;
    public string MerchantSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public string ListingStatus { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public string? CategoryId { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Hepsiburada siparis listesi response.
/// </summary>
public class HbOrderListResponse
{
    public List<HbOrder> Orders { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Hepsiburada siparis modeli.
/// </summary>
public class HbOrder
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PackageNumber { get; set; }
    public List<HbOrderItem> Lines { get; set; } = new();
}

/// <summary>
/// Hepsiburada siparis kalemi.
/// </summary>
public class HbOrderItem
{
    public string MerchantSku { get; set; } = string.Empty;
    public string HepsiburadaSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; }
}

// ── Claims ────────────────────────────────────────────

/// <summary>
/// Hepsiburada claim listesi response.
/// </summary>
public class HbClaimListResponse
{
    public List<HbClaimDto> Claims { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Hepsiburada iade/claim modeli.
/// </summary>
public record HbClaimDto(
    string Id,
    string OrderId,
    string Reason,
    string Status,
    decimal Amount);

// ── Upload Status ─────────────────────────────────────

/// <summary>
/// Hepsiburada listing upload status modeli.
/// </summary>
public record HbUploadStatusDto(
    string CorrelationId,
    string Status,
    int TotalItems,
    int SuccessCount,
    int FailureCount,
    List<string>? Errors);

// ── Commissions ───────────────────────────────────────

/// <summary>
/// Hepsiburada komisyon listesi response.
/// </summary>
public class HbCommissionListResponse
{
    public List<HbCommissionDto> Commissions { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Hepsiburada komisyon modeli.
/// </summary>
public record HbCommissionDto(
    string Category,
    decimal Rate,
    decimal Amount);

// ── Tracking ──────────────────────────────────────────

/// <summary>
/// Hepsiburada kargo takip modeli.
/// </summary>
public record HbTrackingDto(
    string TrackingNumber,
    string CargoCompany,
    string Status,
    DateTime? LastUpdateDate,
    List<HbTrackingEvent>? Events);

/// <summary>
/// Hepsiburada kargo takip olay modeli.
/// </summary>
public record HbTrackingEvent(
    string Description,
    string Location,
    DateTime EventDate);
