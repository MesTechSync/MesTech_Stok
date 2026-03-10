using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

public class Quotation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string QuotationNumber { get; set; } = string.Empty;
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }

    // Customer info
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerTaxOffice { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }

    // Amounts
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "TRY";

    // Notes / Terms
    public string? Notes { get; set; }
    public string? Terms { get; set; }

    // Conversion
    public Guid? ConvertedInvoiceId { get; set; }

    // Lines
    private readonly List<QuotationLine> _lines = new();
    public IReadOnlyCollection<QuotationLine> Lines => _lines.AsReadOnly();

    // Navigation
    public Customer? Customer { get; set; }
    public Invoice? ConvertedInvoice { get; set; }

    public void AddLine(QuotationLine line)
    {
        _lines.Add(line);
        CalculateTotals();
    }

    public void CalculateTotals()
    {
        SubTotal = _lines.Sum(l => l.LineTotal);
        TaxTotal = _lines.Sum(l => l.TaxAmount);
        GrandTotal = SubTotal + TaxTotal;
    }

    public void Send()
    {
        if (Status != QuotationStatus.Draft)
            throw new InvalidOperationException(
                $"Quotation can only be sent from Draft status. Current status: {Status}.");

        Status = QuotationStatus.Sent;
    }

    public void Accept()
    {
        if (Status != QuotationStatus.Sent)
            throw new InvalidOperationException(
                $"Quotation can only be accepted from Sent status. Current status: {Status}.");

        Status = QuotationStatus.Accepted;
    }

    public void Reject()
    {
        if (Status != QuotationStatus.Sent)
            throw new InvalidOperationException(
                $"Quotation can only be rejected from Sent status. Current status: {Status}.");

        Status = QuotationStatus.Rejected;
    }

    public void MarkAsExpired()
    {
        if (Status == QuotationStatus.Accepted || Status == QuotationStatus.Converted)
            return;

        Status = QuotationStatus.Expired;
    }

    public void MarkAsConverted(Guid invoiceId)
    {
        if (Status != QuotationStatus.Accepted)
            throw new InvalidOperationException(
                $"Quotation can only be converted from Accepted status. Current status: {Status}.");

        Status = QuotationStatus.Converted;
        ConvertedInvoiceId = invoiceId;
    }
}
