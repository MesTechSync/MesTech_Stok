using System.Globalization;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Entities;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.Logo;

/// <summary>
/// Maps MesTech domain entities to Logo REST API DTOs.
/// Invoice → LogoSalesInvoiceRequest (salesInvoices)
/// AccountingExpenseDto → LogoPurchaseInvoiceRequest (purchaseInvoices)
/// CounterpartyDto → LogoCurrentAccountRequest (currentAccounts)
/// Order → LogoSalesOrderRequest (salesOrders) — Dalga 12
/// </summary>
internal static class LogoMappingProfile
{
    /// <summary>
    /// Maps a MesTech Invoice to Logo sales invoice request.
    /// </summary>
    internal static LogoSalesInvoiceRequest MapInvoice(InvoiceEntity invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new LogoSalesInvoiceRequest
        {
            InvoiceNumber = invoice.InvoiceNumber,
            Description = $"MesTech Invoice #{invoice.InvoiceNumber}",
            IssueDate = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DueDate = invoice.InvoiceDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Currency = invoice.Currency,
            NetTotal = invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
            TaxTotal = invoice.TaxTotal.ToString("F2", CultureInfo.InvariantCulture),
            GrossTotal = invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture),
            CustomerName = invoice.CustomerName,
            CustomerTaxNumber = invoice.CustomerTaxNumber
        };
    }

    /// <summary>
    /// Maps a MesTech AccountingExpenseDto to Logo purchase invoice request.
    /// </summary>
    internal static LogoPurchaseInvoiceRequest MapExpense(AccountingExpenseDto expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new LogoPurchaseInvoiceRequest
        {
            Description = expense.Title,
            IssueDate = expense.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DueDate = expense.ExpenseDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Currency = "TRY",
            NetTotal = expense.Amount.ToString("F2", CultureInfo.InvariantCulture),
            Category = expense.Category
        };
    }

    /// <summary>
    /// Maps a MesTech CounterpartyDto to Logo current account request.
    /// Upsert by tax number (VKN).
    /// </summary>
    internal static LogoCurrentAccountRequest MapCounterparty(CounterpartyDto party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return new LogoCurrentAccountRequest
        {
            Code = party.VKN ?? party.Id.ToString("N"),
            Title = party.Name,
            TaxNumber = party.VKN,
            AccountType = party.CounterpartyType.Equals("supplier", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
            Phone = party.Phone,
            Email = party.Email,
            Address = party.Address
        };
    }

    /// <summary>
    /// Maps a MesTech Order to Logo sales order request.
    /// Dalga 12: IErpAdapter.SyncOrderAsync support.
    /// </summary>
    internal static LogoSalesOrderRequest MapOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var request = new LogoSalesOrderRequest
        {
            OrderNumber = order.OrderNumber,
            Description = $"MesTech Order #{order.OrderNumber}",
            OrderDate = order.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RequiredDate = order.RequiredDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Currency = "TRY",
            SubTotal = order.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
            TaxTotal = order.TaxAmount.ToString("F2", CultureInfo.InvariantCulture),
            GrossTotal = order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            Status = order.Status.ToString(),
            SourceOrderId = order.ExternalOrderId
        };

        foreach (var item in order.OrderItems)
        {
            request.Lines.Add(new LogoSalesOrderLineRequest
            {
                ProductCode = !string.IsNullOrWhiteSpace(item.ProductSKU) ? item.ProductSKU : item.ProductId.ToString("N"),
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture),
                TotalPrice = item.TotalPrice.ToString("F2", CultureInfo.InvariantCulture),
                TaxRate = item.TaxRate.ToString("F2", CultureInfo.InvariantCulture)
            });
        }

        return request;
    }
}
