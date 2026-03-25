using System.Globalization;
using System.Text.Json.Serialization;
using MesTech.Application.DTOs.Accounting;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.BizimHesap;

/// <summary>
/// Maps MesTech domain entities to BizimHesap REST API DTOs.
/// Invoice → BizimHesapInvoiceRequest (POST /invoices)
/// AccountingExpenseDto → BizimHesapExpenseRequest (POST /expenses)
/// CounterpartyDto → BizimHesapContactRequest (POST /contacts)
/// </summary>
internal static class BizimHesapMappingProfile
{
    /// <summary>
    /// Maps a MesTech Invoice to BizimHesap invoice request.
    /// </summary>
    internal static BizimHesapInvoiceRequest MapInvoice(InvoiceEntity invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new BizimHesapInvoiceRequest
        {
            InvoiceNumber = invoice.InvoiceNumber,
            Description = $"MesTech Invoice #{invoice.InvoiceNumber}",
            IssueDate = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DueDate = invoice.InvoiceDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Currency = invoice.Currency,
            SubTotal = invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
            TaxTotal = invoice.TaxTotal.ToString("F2", CultureInfo.InvariantCulture),
            GrandTotal = invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture),
            CustomerName = invoice.CustomerName,
            CustomerTaxNumber = invoice.CustomerTaxNumber
        };
    }

    /// <summary>
    /// Maps a MesTech AccountingExpenseDto to BizimHesap expense request.
    /// </summary>
    internal static BizimHesapExpenseRequest MapExpense(AccountingExpenseDto expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new BizimHesapExpenseRequest
        {
            Title = expense.Title,
            Amount = expense.Amount.ToString("F2", CultureInfo.InvariantCulture),
            Category = expense.Category,
            ExpenseDate = expense.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Currency = "TRY"
        };
    }

    /// <summary>
    /// Maps a MesTech CounterpartyDto to BizimHesap contact request.
    /// Upsert by VKN (tax number).
    /// </summary>
    internal static BizimHesapContactRequest MapCounterparty(CounterpartyDto party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return new BizimHesapContactRequest
        {
            Name = party.Name,
            TaxNumber = party.VKN,
            ContactType = party.CounterpartyType.Equals("supplier", StringComparison.OrdinalIgnoreCase)
                ? "supplier"
                : "customer",
            Phone = party.Phone,
            Email = party.Email,
            Address = party.Address,
            IsActive = party.IsActive
        };
    }
}

// ── BizimHesap Request Models ──────────────────────────────────────

/// <summary>
/// POST /invoices — invoice creation.
/// </summary>
internal sealed class BizimHesapInvoiceRequest
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

    [JsonPropertyName("subTotal")]
    public string SubTotal { get; set; } = "0.00";

    [JsonPropertyName("taxTotal")]
    public string? TaxTotal { get; set; }

    [JsonPropertyName("grandTotal")]
    public string? GrandTotal { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerTaxNumber")]
    public string? CustomerTaxNumber { get; set; }
}

/// <summary>
/// POST /expenses — expense creation.
/// </summary>
internal sealed class BizimHesapExpenseRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("expenseDate")]
    public string ExpenseDate { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";
}

/// <summary>
/// POST /contacts — contact (customer/supplier) upsert by VKN.
/// </summary>
internal sealed class BizimHesapContactRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("contactType")]
    public string ContactType { get; set; } = "customer";

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// GET /companies/me — company info response.
/// </summary>
internal sealed class BizimHesapCompanyResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// GET /accounts/{code} — account balance response.
/// </summary>
internal sealed class BizimHesapAccountResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("balance")]
    public string? Balance { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

/// <summary>
/// Invoice response from BizimHesap API (GET/POST /api/v1/invoices).
/// </summary>
internal sealed class BizimHesapInvoiceResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("invoiceDate")]
    public DateTime? InvoiceDate { get; set; }

    [JsonPropertyName("grandTotal")]
    public string? GrandTotal { get; set; }

    [JsonPropertyName("pdfUrl")]
    public string? PdfUrl { get; set; }
}

/// <summary>
/// Contact response from BizimHesap API (GET/POST /api/v1/contacts).
/// </summary>
internal sealed class BizimHesapContactResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("balance")]
    public string? Balance { get; set; }
}

/// <summary>
/// Price item response from BizimHesap API (GET /api/v1/stock-items — includes price fields).
/// </summary>
internal sealed class BizimHesapPriceItemResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("purchasePrice")]
    public decimal PurchasePrice { get; set; }

    [JsonPropertyName("salePrice")]
    public decimal SalePrice { get; set; }

    [JsonPropertyName("listPrice")]
    public decimal? ListPrice { get; set; }

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }
}

/// <summary>
/// Stock item response from BizimHesap API (GET /api/v1/stock-items).
/// </summary>
internal sealed class BizimHesapStockItemResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitCode")]
    public string? UnitCode { get; set; }

    [JsonPropertyName("warehouseCode")]
    public string? WarehouseCode { get; set; }

    [JsonPropertyName("unitCost")]
    public string? UnitCost { get; set; }
}

/// <summary>
/// Waybill response from BizimHesap API.
/// </summary>
internal sealed class BizimHesapWaybillResponse
{
    [JsonPropertyName("waybillNumber")]
    public string? WaybillNumber { get; set; }

    [JsonPropertyName("waybillDate")]
    public DateTime? WaybillDate { get; set; }
}

/// <summary>
/// Bank transaction response from BizimHesap API.
/// </summary>
internal sealed class BizimHesapBankTransactionResponse
{
    [JsonPropertyName("transactionDate")]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}

/// <summary>
/// Payment response from BizimHesap API.
/// </summary>
internal sealed class BizimHesapPaymentResponse
{
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}
