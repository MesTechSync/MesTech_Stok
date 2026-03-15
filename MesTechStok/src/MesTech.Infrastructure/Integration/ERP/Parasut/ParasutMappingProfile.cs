using System.Globalization;
using MesTech.Application.DTOs.Accounting;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.Parasut;

/// <summary>
/// Maps MesTech domain entities to Parasut JSON:API DTOs.
/// Invoice → ParasutInvoiceAttributes (sales_invoices)
/// AccountingExpenseDto → ParasutPurchaseBillAttributes (purchase_bills)
/// CounterpartyDto → ParasutContactAttributes (contacts)
/// </summary>
internal static class ParasutMappingProfile
{
    /// <summary>
    /// Maps a MesTech Invoice to Parasut sales_invoice attributes.
    /// </summary>
    internal static ParasutJsonApiRequest<ParasutInvoiceAttributes> MapInvoice(InvoiceEntity invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new ParasutJsonApiRequest<ParasutInvoiceAttributes>
        {
            Data = new ParasutDataWrapper<ParasutInvoiceAttributes>
            {
                Type = "sales_invoices",
                Attributes = new ParasutInvoiceAttributes
                {
                    Description = $"MesTech Invoice #{invoice.InvoiceNumber}",
                    IssueDate = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DueDate = invoice.InvoiceDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    InvoiceSeries = invoice.InvoiceNumber,
                    Currency = invoice.Currency == "TRY" ? "TRL" : invoice.Currency,
                    NetTotal = invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
                    TaxTotal = invoice.TaxTotal.ToString("F2", CultureInfo.InvariantCulture),
                    GrossTotal = invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        };
    }

    /// <summary>
    /// Maps a MesTech AccountingExpenseDto to Parasut purchase_bill attributes.
    /// </summary>
    internal static ParasutJsonApiRequest<ParasutPurchaseBillAttributes> MapExpense(AccountingExpenseDto expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new ParasutJsonApiRequest<ParasutPurchaseBillAttributes>
        {
            Data = new ParasutDataWrapper<ParasutPurchaseBillAttributes>
            {
                Type = "purchase_bills",
                Attributes = new ParasutPurchaseBillAttributes
                {
                    Description = expense.Title,
                    IssueDate = expense.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DueDate = expense.ExpenseDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Currency = "TRL",
                    NetTotal = expense.Amount.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        };
    }

    /// <summary>
    /// Maps a MesTech CounterpartyDto to Parasut contact attributes.
    /// Upsert by VKN (tax_number).
    /// </summary>
    internal static ParasutJsonApiRequest<ParasutContactAttributes> MapCounterparty(CounterpartyDto party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return new ParasutJsonApiRequest<ParasutContactAttributes>
        {
            Data = new ParasutDataWrapper<ParasutContactAttributes>
            {
                Type = "contacts",
                Attributes = new ParasutContactAttributes
                {
                    Name = party.Name,
                    ContactType = string.IsNullOrEmpty(party.VKN) ? "person" : "company",
                    TaxNumber = party.VKN,
                    Phone = party.Phone,
                    Email = party.Email,
                    Address = party.Address,
                    AccountType = party.CounterpartyType.Equals("supplier", StringComparison.OrdinalIgnoreCase)
                        ? "supplier"
                        : "customer"
                }
            }
        };
    }
}
