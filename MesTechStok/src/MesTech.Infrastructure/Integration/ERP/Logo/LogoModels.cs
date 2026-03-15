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
