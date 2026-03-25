using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.ERP.Logo;

/// <summary>
/// Logo REST API request/response DTOs.
/// L-Object REST API uses standard JSON (not JSON:API).
/// </summary>

// ── Request Models ──────────────────────────────────────────────────

/// <summary>
/// POST /salesInvoices — sales invoice creation.
/// </summary>
internal sealed class LogoSalesInvoiceRequest
{
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("issueDate")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    [JsonPropertyName("netTotal")]
    public string NetTotal { get; set; } = "0.00";

    [JsonPropertyName("taxTotal")]
    public string? TaxTotal { get; set; }

    [JsonPropertyName("grossTotal")]
    public string? GrossTotal { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerTaxNumber")]
    public string? CustomerTaxNumber { get; set; }
}

/// <summary>
/// POST /purchaseInvoices — purchase/expense invoice creation.
/// </summary>
internal sealed class LogoPurchaseInvoiceRequest
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("issueDate")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    [JsonPropertyName("netTotal")]
    public string NetTotal { get; set; } = "0.00";

    [JsonPropertyName("taxTotal")]
    public string? TaxTotal { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

/// <summary>
/// POST /currentAccounts — current account (customer/supplier) upsert by tax number.
/// </summary>
internal sealed class LogoCurrentAccountRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("accountType")]
    public int AccountType { get; set; } = 1; // 1=Customer, 2=Supplier

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

// ── Response Models ──────────────────────────────────────────────────

/// <summary>
/// Token response from POST /api/v1/token.
/// </summary>
internal sealed class LogoTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Generic Logo API response wrapper.
/// </summary>
internal sealed class LogoApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Balance response from GET /currentAccounts/{code}/balance.
/// </summary>
internal sealed class LogoBalanceResponse
{
    [JsonPropertyName("balance")]
    public string? Balance { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

// ── Dalga 12: IErpAdapter Models ────────────────────────────────────

/// <summary>
/// POST /salesOrders — sales order creation for IErpAdapter.SyncOrderAsync.
/// Maps MesTech Order entity to Logo L-Object salesOrders endpoint.
/// </summary>
internal sealed class LogoSalesOrderRequest
{
    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("orderDate")]
    public string OrderDate { get; set; } = string.Empty;

    [JsonPropertyName("requiredDate")]
    public string? RequiredDate { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    [JsonPropertyName("subTotal")]
    public string SubTotal { get; set; } = "0.00";

    [JsonPropertyName("taxTotal")]
    public string TaxTotal { get; set; } = "0.00";

    [JsonPropertyName("grossTotal")]
    public string GrossTotal { get; set; } = "0.00";

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerEmail")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("sourceOrderId")]
    public string? SourceOrderId { get; set; }

    [JsonPropertyName("lines")]
    public List<LogoSalesOrderLineRequest> Lines { get; set; } = new();
}

/// <summary>
/// Sales order line item — part of LogoSalesOrderRequest.
/// </summary>
internal sealed class LogoSalesOrderLineRequest
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public string UnitPrice { get; set; } = "0.00";

    [JsonPropertyName("totalPrice")]
    public string TotalPrice { get; set; } = "0.00";

    [JsonPropertyName("taxRate")]
    public string TaxRate { get; set; } = "0.00";
}

/// <summary>
/// Generic POST response with record ID — used by SyncOrderAsync and SyncInvoiceAsync
/// to extract the ERP-side reference number.
/// </summary>
internal sealed class LogoCreateResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// GET /currentAccounts/balances — list of account balances.
/// Used by IErpAdapter.GetAccountBalancesAsync.
/// </summary>
internal sealed class LogoAccountBalancesResponse
{
    [JsonPropertyName("accounts")]
    public List<LogoAccountBalanceItem> Accounts { get; set; } = new();
}

/// <summary>
/// Single account balance item in the balances list.
/// </summary>
internal sealed class LogoAccountBalanceItem
{
    [JsonPropertyName("accountCode")]
    public string AccountCode { get; set; } = string.Empty;

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public string Balance { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";
}

// ── ISP Capability Models ─────────────────────────────────────────────

/// <summary>
/// GET /salesInvoices/{number} — invoice detail response.
/// </summary>
internal sealed class LogoInvoiceDetailResponse
{
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("erpRef")]
    public string? ErpRef { get; set; }

    [JsonPropertyName("issueDate")]
    public string? IssueDate { get; set; }

    [JsonPropertyName("grossTotal")]
    public string? GrossTotal { get; set; }

    [JsonPropertyName("pdfUrl")]
    public string? PdfUrl { get; set; }
}

/// <summary>
/// GET /salesInvoices?filter=DATE_ — invoice list wrapper.
/// </summary>
internal sealed class LogoInvoiceListResponse
{
    [JsonPropertyName("items")]
    public List<LogoInvoiceDetailResponse> Items { get; set; } = new();
}

/// <summary>
/// GET /currentAccounts/{code} — account detail response.
/// </summary>
internal sealed class LogoAccountDetailResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public string Balance { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

/// <summary>
/// GET /currentAccounts?filter= — account search wrapper.
/// </summary>
internal sealed class LogoAccountSearchResponse
{
    [JsonPropertyName("items")]
    public List<LogoAccountDetailResponse> Items { get; set; } = new();
}

/// <summary>
/// GET /items — stock item response.
/// </summary>
internal sealed class LogoStockItemResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitCode")]
    public string UnitCode { get; set; } = "ADET";

    [JsonPropertyName("warehouseCode")]
    public string? WarehouseCode { get; set; }

    [JsonPropertyName("unitCost")]
    public string? UnitCost { get; set; }
}

/// <summary>
/// GET /items — stock list wrapper.
/// </summary>
internal sealed class LogoStockListResponse
{
    [JsonPropertyName("items")]
    public List<LogoStockItemResponse> Items { get; set; } = new();
}

/// <summary>
/// POST /items/{code}/inventory — stock update request.
/// </summary>
internal sealed class LogoStockUpdateRequest
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("warehouseCode")]
    public string WarehouseCode { get; set; } = string.Empty;
}

/// <summary>
/// GET /items/prices — price list item from Logo.
/// </summary>
internal sealed class LogoPriceItemResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("purchasePrice")]
    public decimal PurchasePrice { get; set; }

    [JsonPropertyName("salePrice")]
    public decimal SalePrice { get; set; }

    [JsonPropertyName("listPrice")]
    public decimal? ListPrice { get; set; }

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "TRY";
}

/// <summary>
/// GET /items/prices — price list wrapper.
/// </summary>
internal sealed class LogoPriceListResponse
{
    [JsonPropertyName("items")]
    public List<LogoPriceItemResponse> Items { get; set; } = new();
}

/// <summary>
/// POST /salesDispatches — waybill creation request.
/// </summary>
internal sealed class LogoSalesDispatchRequest
{
    [JsonPropertyName("customerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("shippingAddress")]
    public string? ShippingAddress { get; set; }

    [JsonPropertyName("cargoFirm")]
    public string? CargoFirm { get; set; }

    [JsonPropertyName("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("lines")]
    public List<LogoSalesDispatchLineRequest> Lines { get; set; } = new();
}

/// <summary>
/// Sales dispatch line item.
/// </summary>
internal sealed class LogoSalesDispatchLineRequest
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitCode")]
    public string UnitCode { get; set; } = "ADET";
}

/// <summary>
/// GET /salesDispatches/{number} — waybill detail response.
/// </summary>
internal sealed class LogoWaybillDetailResponse
{
    [JsonPropertyName("waybillNumber")]
    public string WaybillNumber { get; set; } = string.Empty;

    [JsonPropertyName("waybillDate")]
    public string? WaybillDate { get; set; }
}

/// <summary>
/// GET /bankSlips — bank transaction response.
/// </summary>
internal sealed class LogoBankTransactionResponse
{
    [JsonPropertyName("transactionDate")]
    public string TransactionDate { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("transactionType")]
    public string TransactionType { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}

/// <summary>
/// GET /bankSlips — bank transaction list wrapper.
/// </summary>
internal sealed class LogoBankTransactionListResponse
{
    [JsonPropertyName("items")]
    public List<LogoBankTransactionResponse> Items { get; set; } = new();
}

/// <summary>
/// POST /bankSlips — bank payment request.
/// </summary>
internal sealed class LogoBankPaymentRequest
{
    [JsonPropertyName("accountCode")]
    public string AccountCode { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("paymentType")]
    public string PaymentType { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
