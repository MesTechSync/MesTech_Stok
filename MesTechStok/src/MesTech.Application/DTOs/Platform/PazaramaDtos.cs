namespace MesTech.Application.DTOs.Platform;

// ──────────────────────────────────────────────
//  Generic API wrapper
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama generic API response wrapper.
/// </summary>
public class PzApiResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string? MessageCode { get; set; }
    public string? Message { get; set; }
    public string? UserMessage { get; set; }
    public bool FromCache { get; set; }
}

// ──────────────────────────────────────────────
//  Product DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama batch product create request (async — returns batchRequestId).
/// </summary>
public class PzProductCreateRequest
{
    public List<PzProductDetail> Products { get; set; } = new();
}

/// <summary>
/// Pazarama batch product create response.
/// </summary>
public class PzProductCreateResponse
{
    public Guid BatchRequestId { get; set; }
    public DateTime CreationDate { get; set; }
}

/// <summary>
/// Pazarama batch result polling response.
/// Status: 1=InProgress, 2=Done, 3=Error.
/// </summary>
public class PzBatchResultResponse
{
    public int Status { get; set; }
    public List<PzBatchResultItem> BatchResult { get; set; } = new();
    public int TotalCount { get; set; }
    public int FailedCount { get; set; }
}

/// <summary>
/// Single item result within a batch operation.
/// </summary>
public class PzBatchResultItem
{
    public string? Code { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Pazarama product list response.
/// </summary>
public class PzProductListResponse
{
    public List<PzProduct> Data { get; set; } = new();
    public bool Success { get; set; }
    public bool FromCache { get; set; }
}

/// <summary>
/// Pazarama product model.
/// </summary>
public class PzProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int StockCount { get; set; }
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int State { get; set; }
    public string? GroupCode { get; set; }
}

/// <summary>
/// Pazarama product detail (extends PzProduct with creation fields).
/// </summary>
public class PzProductDetail : PzProduct
{
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
    public List<PzAttribute> Attributes { get; set; } = new();
    public List<PzImage> Images { get; set; } = new();
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal? VatRate { get; set; }
    public int? DeliveryType { get; set; }
    public int? DeliveryMessageType { get; set; }
}

/// <summary>
/// Pazarama product attribute.
/// </summary>
public class PzAttribute
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Pazarama product image.
/// </summary>
public class PzImage
{
    public string Url { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

// ──────────────────────────────────────────────
//  Price / Stock update
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama price update request.
/// </summary>
public class PzPriceUpdateRequest
{
    public List<PzPriceItem> Items { get; set; } = new();
}

/// <summary>
/// Pazarama price item.
/// </summary>
public class PzPriceItem
{
    public string Code { get; set; } = string.Empty;
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
}

/// <summary>
/// Pazarama stock update request.
/// </summary>
public class PzStockUpdateRequest
{
    public List<PzStockItem> Items { get; set; } = new();
}

/// <summary>
/// Pazarama stock item.
/// </summary>
public class PzStockItem
{
    public string Code { get; set; } = string.Empty;
    public int StockCount { get; set; }
}

// ──────────────────────────────────────────────
//  Order DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama order list request (POST, date range required, max 1 month).
/// </summary>
public class PzOrderListRequest
{
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long? OrderNumber { get; set; }
}

/// <summary>
/// Pazarama order list response.
/// </summary>
public class PzOrderListResponse
{
    public List<PzOrder> Data { get; set; } = new();
    public bool Success { get; set; }
}

/// <summary>
/// Pazarama order model.
/// </summary>
public class PzOrder
{
    public Guid OrderId { get; set; }
    public long OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal OrderAmount { get; set; }
    public int OrderStatus { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public PzAddress? ShipmentAddress { get; set; }
    public PzAddress? BillingAddress { get; set; }
    public List<PzOrderItem> Items { get; set; } = new();
}

/// <summary>
/// Pazarama order item.
/// </summary>
public class PzOrderItem
{
    public Guid OrderItemId { get; set; }
    public int OrderItemStatus { get; set; }
    public int Quantity { get; set; }
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public PzCargoInfo? Cargo { get; set; }
    public PzProductInfo? Product { get; set; }
    public int DeliveryType { get; set; }
    public string? ShipmentCode { get; set; }
}

/// <summary>
/// Pazarama cargo information within an order item.
/// </summary>
public class PzCargoInfo
{
    public string? CompanyName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
}

/// <summary>
/// Pazarama product info within an order item.
/// </summary>
public class PzProductInfo
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal VatRate { get; set; }
    public string? ImageURL { get; set; }
}

/// <summary>
/// Pazarama address model.
/// </summary>
public class PzAddress
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
}

// ──────────────────────────────────────────────
//  Cargo notification
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama order status update request (2-stage: status=12 Hazirlaniyor, status=5 Kargoya Verildi).
/// </summary>
public class PzUpdateOrderStatusRequest
{
    public long OrderNumber { get; set; }
    public PzOrderStatusItem Item { get; set; } = new();
}

/// <summary>
/// Pazarama order status item for cargo notification.
/// </summary>
public class PzOrderStatusItem
{
    public Guid OrderItemId { get; set; }
    public int Status { get; set; }
    public int DeliveryType { get; set; }
    public string ShippingTrackingNumber { get; set; } = string.Empty;
    public string? TrackingUrl { get; set; }
    public Guid? CargoCompanyId { get; set; }
}

// ──────────────────────────────────────────────
//  Refund DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama refund list request.
/// </summary>
public class PzRefundListRequest
{
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
    public int? RefundStatus { get; set; }
    public DateTime? RequestStartDate { get; set; }
    public DateTime? RequestEndDate { get; set; }
}

/// <summary>
/// Pazarama refund list response.
/// </summary>
public class PzRefundListResponse
{
    public PzRefundData? Data { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Pazarama refund data container with pagination info.
/// </summary>
public class PzRefundData
{
    public int ResponsePage { get; set; }
    public int PageReport { get; set; }
    public List<PzRefund> RefundList { get; set; } = new();
}

/// <summary>
/// Pazarama refund model.
/// </summary>
public class PzRefund
{
    public Guid RefundId { get; set; }
    public long OrderNumber { get; set; }
    public string RefundNumber { get; set; } = string.Empty;
    public int RefundType { get; set; }
    public int RefundStatus { get; set; }
    public decimal RefundAmount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ShipmentCode { get; set; }
}

/// <summary>
/// Pazarama refund status update request.
/// Firms can only send status 2 (Onay/Approve) or 3 (Ret/Reject).
/// </summary>
public class PzUpdateRefundRequest
{
    public Guid RefundId { get; set; }
    public int Status { get; set; }
}

// ──────────────────────────────────────────────
//  Invoice link
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama invoice link request.
/// </summary>
public class PzInvoiceLinkRequest
{
    public string InvoiceLink { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid? DeliveryCompanyId { get; set; }
    public string? TrackingNumber { get; set; }
}

// ──────────────────────────────────────────────
//  Category / Brand
// ──────────────────────────────────────────────

/// <summary>
/// Pazarama category tree node.
/// </summary>
public class PzCategoryTree
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Leaf { get; set; }
    public List<PzCategoryTree> ParentCategories { get; set; } = new();
}

/// <summary>
/// Pazarama category with its attributes.
/// </summary>
public class PzCategoryAttributes
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PzCategoryAttribute> Attributes { get; set; } = new();
}

/// <summary>
/// Pazarama category attribute definition.
/// </summary>
public class PzCategoryAttribute
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsVariantable { get; set; }
    public bool IsRequired { get; set; }
    public List<PzAttributeValue> Values { get; set; } = new();
}

/// <summary>
/// Pazarama attribute value.
/// </summary>
public class PzAttributeValue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Pazarama brand model.
/// </summary>
public class PzBrand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
}
