using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Entities;

public sealed class Invoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? StoreId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceStatus Status { get; internal set; } = InvoiceStatus.Draft;
    public InvoiceDirection Direction { get; set; } = InvoiceDirection.Outgoing;
    public InvoiceProvider Provider { get; set; } = InvoiceProvider.None;
    public InvoiceScenario Scenario { get; set; } = InvoiceScenario.Basic;

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerTaxOffice { get; set; }
    public string CustomerAddress { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public bool IsEInvoiceTaxpayer { get; set; }

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string Currency { get; set; } = "TRY";

    public string? PlatformCode { get; set; }
    public string? PlatformOrderId { get; set; }
    public string? PlatformInvoiceUrl { get; private set; }

    public string? GibInvoiceId { get; private set; }
    public string? GibEnvelopeId { get; private set; }
    public string? PdfUrl { get; private set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    // ── Muhasebe Modulu (MUH-01) ──
    public string? GLAccountCode { get; set; }
    public Guid? SettlementBatchId { get; set; }

    // ── e-İrsaliye (İ-08) ──
    public string? WaybillNumber { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string? DriverName { get; set; }
    public string? DriverSurname { get; set; }
    public string? VehiclePlate { get; set; }
    public string? ShipmentAddress { get; set; }
    public Guid? CargoShipmentId { get; set; }

    // ── e-SMM (İ-08) ──
    public string? ProfessionalTitle { get; set; }
    public string? ActivityCode { get; set; }
    public decimal? WithholdingRate { get; set; }
    public decimal? WithholdingAmount { get; set; }

    // ── e-İhracat (İ-08) ──
    public string? GtipCode { get; set; }
    public string? CustomsDeclarationNo { get; set; }
    public string? ExportCurrency { get; set; }
    public decimal? ExportExchangeRate { get; set; }
    public string? ExemptionCode { get; set; }

    // ── Dijital İmza (İ-08) ──
    public SignatureStatus SignatureStatus { get; private set; } = SignatureStatus.Unsigned;
    public DateTime? SignedAt { get; private set; }
    public string? SignedBy { get; private set; }
    public SignatureType? SignatureType { get; private set; }
    public string? GibStatus { get; private set; }
    public DateTime? GibStatusDate { get; private set; }

    // ── Paraşüt Sync (İ-08) ──
    public string? ParasutSalesInvoiceId { get; private set; }
    public string? ParasutEInvoiceId { get; private set; }
    public SyncStatus? ParasutSyncStatus { get; private set; }
    public DateTime? ParasutSyncedAt { get; private set; }
    public string? ParasutSyncError { get; private set; }

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

    public void SetFinancials(decimal subTotal, decimal taxTotal, decimal grandTotal)
    {
        SubTotal = subTotal;
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
    }

    public void MarkAsSent(string? gibInvoiceId, string? pdfUrl)
    {
        Status = InvoiceStatus.Sent;
        GibInvoiceId = gibInvoiceId;
        PdfUrl = pdfUrl;
        SentAt = DateTime.UtcNow;
        RaiseDomainEvent(new InvoiceSentEvent(Id, TenantId, gibInvoiceId, pdfUrl, DateTime.UtcNow));
    }

    public void MarkAsAccepted()
    {
        Status = InvoiceStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        RaiseDomainEvent(new InvoiceAcceptedEvent(Id, TenantId, InvoiceNumber, GrandTotal, DateTime.UtcNow));
    }

    public void MarkAsRejected()
    {
        Status = InvoiceStatus.Rejected;
        RaiseDomainEvent(new InvoiceRejectedEvent(Id, TenantId, InvoiceNumber, DateTime.UtcNow));
    }

    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public void Cancel(string? reason = null)
    {
        if (Status == InvoiceStatus.Accepted)
            throw new BusinessRuleException("InvoiceRule","Kabul edilmis fatura iptal edilemez.");
        Status = InvoiceStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        RaiseDomainEvent(new InvoiceCancelledEvent(Id, TenantId, OrderId, InvoiceNumber, reason, DateTime.UtcNow));
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
            invoice.Id, order.Id, order.TenantId, type, order.TotalAmount, DateTime.UtcNow));
        return invoice;
    }

    // ══════ EMR-08: DOMAIN IS KURALLARI ══════

    /// <summary>
    /// Fatura tipini alici bilgisine gore otomatik belirle.
    /// VKN varsa → e-Fatura (mukellef varsayimi), yurt disi platform → e-Ihracat, diger → e-Arsiv.
    /// GIB mukellef listesi dogrulamasi provider seviyesinde yapilir.
    /// </summary>
    public void DetermineInvoiceType()
    {
        if (!string.IsNullOrEmpty(CustomerTaxNumber) && CustomerTaxNumber.Length == 10)
        {
            // VKN (10 hane) → e-Fatura (mukellef varsayimi)
            Type = InvoiceType.EFatura;
            Scenario = InvoiceScenario.Commercial;
            IsEInvoiceTaxpayer = true;
        }
        else if (PlatformCode == PlatformType.AmazonEu.ToString()
              || PlatformCode == PlatformType.eBay.ToString()
              || PlatformCode == PlatformType.Etsy.ToString()
              || PlatformCode == PlatformType.Ozon.ToString())
        {
            // Yurt disi platform → e-Ihracat
            Type = InvoiceType.EIhracat;
            Scenario = InvoiceScenario.Export;
        }
        else
        {
            // Bireysel alici (TCKN veya anonim) → e-Arsiv
            Type = InvoiceType.EArsiv;
            Scenario = InvoiceScenario.Basic;
        }
    }

    /// <summary>
    /// Faturayi onayla ve gonderime hazirla.
    /// Sadece taslak, kalemli ve pozitif tutarli fatura onaylanabilir.
    /// </summary>
    public void Approve()
    {
        if (Status != InvoiceStatus.Draft)
            throw new BusinessRuleException("InvoiceRule",$"Sadece taslak fatura onaylanabilir. Mevcut durum: {Status}");
        if (_lines.Count == 0)
            throw new BusinessRuleException("InvoiceRule","Fatura kalemsiz onaylanamaz.");
        if (GrandTotal <= 0)
            throw new BusinessRuleException("InvoiceRule","Fatura tutari sifirdan buyuk olmali.");

        Status = InvoiceStatus.Queued;
        RaiseDomainEvent(new InvoiceApprovedEvent(Id, TenantId, InvoiceNumber, GrandTotal, Type, DateTime.UtcNow));
        RaiseDomainEvent(new InvoiceGeneratedForERPEvent(Id, TenantId, InvoiceNumber, GrandTotal, "Default", DateTime.UtcNow));
    }

    public void MarkParasutSynced(string salesInvoiceId, string? eInvoiceId)
    {
        ParasutSalesInvoiceId = salesInvoiceId;
        ParasutEInvoiceId = eInvoiceId;
        ParasutSyncStatus = SyncStatus.Synced;
        ParasutSyncedAt = DateTime.UtcNow;
        ParasutSyncError = null;
    }

    public void MarkParasutFailed(string error)
    {
        ParasutSyncStatus = SyncStatus.Failed;
        ParasutSyncError = error.Length > 500 ? error[..500] : error;
    }

    public void Sign(string signedBy, SignatureType signatureType)
    {
        SignatureStatus = SignatureStatus.Signed;
        SignedAt = DateTime.UtcNow;
        SignedBy = signedBy;
        SignatureType = signatureType;
    }

    public void UpdateGibStatus(string status, string? envelopeId = null)
    {
        GibStatus = status;
        GibStatusDate = DateTime.UtcNow;
        if (envelopeId != null) GibEnvelopeId = envelopeId;
    }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    public override string ToString() => $"Invoice #{InvoiceNumber} ({Status}) - {GrandTotal:C}";
}
