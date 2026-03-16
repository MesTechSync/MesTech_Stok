namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Ciceksepeti API response modelleri — adapter icinde kullanilir.
/// </summary>
public class CsProductListResponse
{
    public List<CsProduct> Products { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Ciceksepeti urun modeli.
/// </summary>
public class CsProduct
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string StockCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SalesPrice { get; set; }
    public decimal? ListPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Description { get; set; }
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<string> Images { get; set; } = new();
}

/// <summary>
/// Ciceksepeti siparis listesi response.
/// </summary>
public class CsOrderListResponse
{
    public List<CsOrder> Orders { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Ciceksepeti siparis modeli.
/// </summary>
public class CsOrder
{
    public long OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public List<CsSubOrder> SubOrders { get; set; } = new();
}

/// <summary>
/// Ciceksepeti alt siparis modeli.
/// </summary>
public class CsSubOrder
{
    public long SubOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CargoCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public List<CsOrderItem> Items { get; set; } = new();
}

/// <summary>
/// Ciceksepeti siparis kalemi.
/// </summary>
public class CsOrderItem
{
    public long ItemId { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// Ciceksepeti webhook payload modeli.
/// </summary>
public class CsWebhookPayload
{
    public string? EventType { get; set; }
    public long? OrderId { get; set; }
    public long? SubOrderId { get; set; }
    public string? NewStatus { get; set; }
}

/// <summary>
/// K1c-06: Ciceksepeti iade listesi response.
/// </summary>
public class CsReturnListResponse
{
    public List<CsReturnDto> Returns { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// K1c-06: Ciceksepeti iade modeli.
/// </summary>
public class CsReturnDto
{
    public long ReturnId { get; set; }
    public long SubOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReturnReason { get; set; }
    public string? CustomerNote { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<CsReturnItemDto> Items { get; set; } = new();
}

/// <summary>
/// K1c-06: Ciceksepeti iade kalemi.
/// </summary>
public class CsReturnItemDto
{
    public long ItemId { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// ── Categories ────────────────────────────────────────

/// <summary>
/// Ciceksepeti kategori listesi response.
/// </summary>
public class CsCategoryListResponse
{
    public List<CsCategoryDto> Categories { get; set; } = new();
}

/// <summary>
/// Ciceksepeti kategori modeli.
/// </summary>
public record CsCategoryDto(
    long Id,
    string Name,
    long? ParentId);

/// <summary>
/// Ciceksepeti kategori attribute listesi response.
/// </summary>
public class CsAttributeListResponse
{
    public List<CsAttributeDto> Attributes { get; set; } = new();
}

/// <summary>
/// Ciceksepeti kategori attribute modeli.
/// </summary>
public record CsAttributeDto(
    long Id,
    string Name,
    bool Required,
    string? Type,
    List<string>? AllowedValues);

// ── Product Update ────────────────────────────────────

/// <summary>
/// Ciceksepeti urun guncelleme DTO.
/// </summary>
public record CsProductUpdateDto(
    long ProductId,
    string ProductName,
    decimal SalesPrice,
    int StockQuantity,
    string? Description,
    string? Barcode);

// ── Batch Operations ──────────────────────────────────

/// <summary>
/// Ciceksepeti toplu stok guncelleme DTO.
/// </summary>
public record CsStockUpdate(
    string StockCode,
    int Quantity);

/// <summary>
/// Ciceksepeti toplu fiyat guncelleme DTO.
/// </summary>
public record CsPriceUpdate(
    string StockCode,
    decimal SalesPrice);

// ── Tracking ──────────────────────────────────────────

/// <summary>
/// Ciceksepeti kargo takip response.
/// </summary>
public record CsTrackingDto(
    string OrderId,
    string CargoCompany,
    string TrackingNumber,
    string Status,
    DateTime? LastUpdateDate);
