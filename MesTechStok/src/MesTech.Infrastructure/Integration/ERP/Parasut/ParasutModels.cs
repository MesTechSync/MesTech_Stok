using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Integration.ERP.Parasut;

/// <summary>
/// Parasut JSON:API request/response DTOs.
/// All Parasut API payloads follow JSON:API format: { data: { type: "...", attributes: {...} } }
/// Content-Type: application/vnd.api+json
/// </summary>

// ── Request Models ──────────────────────────────────────────────────

internal sealed class ParasutJsonApiRequest<T>
{
    [JsonPropertyName("data")]
    public ParasutDataWrapper<T> Data { get; set; } = new();
}

internal sealed class ParasutDataWrapper<T>
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public T Attributes { get; set; } = default!;
}

/// <summary>
/// POST /sales_invoices attributes.
/// </summary>
internal sealed class ParasutInvoiceAttributes
{
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = "invoice";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("issue_date")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("invoice_series")]
    public string? InvoiceSeries { get; set; }

    [JsonPropertyName("invoice_id")]
    public int? InvoiceId { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRL";

    [JsonPropertyName("net_total")]
    public string NetTotal { get; set; } = "0.00";

    [JsonPropertyName("tax_total")]
    public string? TaxTotal { get; set; }

    [JsonPropertyName("gross_total")]
    public string? GrossTotal { get; set; }
}

/// <summary>
/// POST /purchase_bills attributes.
/// </summary>
internal sealed class ParasutPurchaseBillAttributes
{
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = "purchase_bill";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("issue_date")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRL";

    [JsonPropertyName("net_total")]
    public string NetTotal { get; set; } = "0.00";

    [JsonPropertyName("tax_total")]
    public string? TaxTotal { get; set; }
}

/// <summary>
/// POST /contacts attributes — customer/supplier/counterparty.
/// Upsert by VKN (tax_number).
/// </summary>
internal sealed class ParasutContactAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contact_type")]
    public string ContactType { get; set; } = "company";

    [JsonPropertyName("tax_number")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("account_type")]
    public string AccountType { get; set; } = "customer";
}

/// <summary>
/// POST /sales_invoices attributes for e-invoice (Dalga 9).
/// Maps EInvoiceDocument GIB fields to Parasut sales_invoice.
/// e_invoice_type: "basic" | "commercial" | "export"
/// </summary>
internal sealed class ParasutEInvoiceAttributes
{
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = "invoice";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("issue_date")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("invoice_series")]
    public string? InvoiceSeries { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRL";

    [JsonPropertyName("net_total")]
    public string NetTotal { get; set; } = "0.00";

    [JsonPropertyName("tax_total")]
    public string? TaxTotal { get; set; }

    [JsonPropertyName("gross_total")]
    public string? GrossTotal { get; set; }

    [JsonPropertyName("e_invoice_type")]
    public string EInvoiceType { get; set; } = "basic";

    [JsonPropertyName("tax_number")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("contact_name")]
    public string? ContactName { get; set; }
}

// ── Response Models ──────────────────────────────────────────────────

internal sealed class ParasutJsonApiResponse
{
    [JsonPropertyName("data")]
    public ParasutResponseData? Data { get; set; }
}

internal sealed class ParasutResponseData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("attributes")]
    public ParasutResponseAttributes? Attributes { get; set; }
}

internal sealed class ParasutResponseAttributes
{
    [JsonPropertyName("balance")]
    public string? Balance { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

internal sealed class ParasutAccountsResponse
{
    [JsonPropertyName("data")]
    public List<ParasutResponseData> Data { get; set; } = new();
}

/// <summary>
/// OAuth2 token response from POST /oauth/token.
/// </summary>
internal sealed class ParasutTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}
