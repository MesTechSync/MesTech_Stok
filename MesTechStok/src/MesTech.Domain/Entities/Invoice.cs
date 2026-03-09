using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

public class Invoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? StoreId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceDirection Direction { get; set; } = InvoiceDirection.Outgoing;
    public InvoiceProvider Provider { get; set; } = InvoiceProvider.None;

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerTaxOffice { get; set; }
    public string CustomerAddress { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public bool IsEInvoiceTaxpayer { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "TRY";

    public string? PlatformCode { get; set; }
    public string? PlatformOrderId { get; set; }
    public string? PlatformInvoiceUrl { get; set; }

    public string? GibInvoiceId { get; set; }
    public string? GibEnvelopeId { get; set; }
    public string? PdfUrl { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    private readonly List<InvoiceLine> _lines = new();
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();
    public Order? Order { get; set; }
    public Store? Store { get; set; }

    public void AddLine(InvoiceLine line)
    {
        _lines.Add(line);
        CalculateTotals();
    }

    public void CalculateTotals()
    {
        SubTotal = _lines.Sum(l => l.UnitPrice * l.Quantity - (l.DiscountAmount ?? 0));
        TaxTotal = _lines.Sum(l => l.TaxAmount);
        GrandTotal = SubTotal + TaxTotal;
    }

    public void MarkAsSent(string? gibInvoiceId, string? pdfUrl)
    {
        Status = InvoiceStatus.Sent;
        GibInvoiceId = gibInvoiceId;
        PdfUrl = pdfUrl;
        SentAt = DateTime.UtcNow;
        RaiseDomainEvent(new InvoiceSentEvent(Id, gibInvoiceId, pdfUrl, DateTime.UtcNow));
    }

    public void MarkAsAccepted()
    {
        Status = InvoiceStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void MarkAsRejected()
    {
        Status = InvoiceStatus.Rejected;
    }

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public void Cancel(string? reason = null)
    {
        if (Status == InvoiceStatus.Accepted)
            throw new InvalidOperationException("Kabul edilmis fatura iptal edilemez.");
        Status = InvoiceStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        RaiseDomainEvent(new InvoiceCancelledEvent(Id, OrderId, InvoiceNumber, reason, DateTime.UtcNow));
    }

    public void MarkAsPlatformSent(string platformInvoiceUrl)
    {
        PlatformInvoiceUrl = platformInvoiceUrl;
        Status = InvoiceStatus.PlatformSent;
    }

    public static Invoice CreateForOrder(Order order, InvoiceType type, string invoiceNumber)
    {
        var invoice = new Invoice
        {
            OrderId = order.Id,
            TenantId = order.TenantId,
            Type = type,
            InvoiceNumber = invoiceNumber,
            CustomerName = order.CustomerName ?? "",
            CustomerEmail = order.CustomerEmail,
            SubTotal = order.SubTotal,
            TaxTotal = order.TaxAmount,
            GrandTotal = order.TotalAmount,
            IsEInvoiceTaxpayer = type == InvoiceType.EFatura
        };
        invoice.RaiseDomainEvent(new InvoiceCreatedEvent(
            invoice.Id, order.Id, type, order.TotalAmount, DateTime.UtcNow));
        return invoice;
    }

    public override string ToString() => $"Invoice #{InvoiceNumber} ({Status}) - {GrandTotal:C}";
}
