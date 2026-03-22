using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.EInvoice;
using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Entities.EInvoice;

public class EInvoiceDocument : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    // GIB Zorunlu Alanlar
    public string GibUuid { get; private set; } = string.Empty;
    public string EttnNo { get; private set; } = string.Empty;
    public EInvoiceScenario Scenario { get; private set; }
    public EInvoiceType Type { get; private set; }
    public EInvoiceStatus Status { get; private set; }

    // Tarih
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }

    // Taraflar
    public string SellerVkn { get; private set; } = string.Empty;
    public string SellerTitle { get; private set; } = string.Empty;
    public string? BuyerVkn { get; private set; }
    public string BuyerTitle { get; private set; } = string.Empty;
    public string? BuyerEmail { get; private set; }

    // Finansal
    public decimal LineExtensionAmount { get; private set; }
    public decimal TaxExclusiveAmount { get; private set; }
    public decimal TaxInclusiveAmount { get; private set; }
    public decimal AllowanceTotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal PayableAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";

    // Tevkifat (KDV Withholding) — GiB KDV Genel Uygulama Tebligi md. I/C-2.1.3.2
    /// <summary>Tevkifat orani (ornegin 0.50 = 5/10). Null ise tevkifat uygulanmaz.</summary>
    public decimal? WithholdingRate { get; private set; }
    /// <summary>Tevkifat tutari = KDV * WithholdingRate.</summary>
    public decimal? WithholdingAmount { get; private set; }
    /// <summary>Net odenecek = PayableAmount - (WithholdingAmount ?? 0).</summary>
    public decimal NetPayable => PayableAmount - (WithholdingAmount ?? 0);

    // Entegrator
    public string ProviderId { get; private set; } = string.Empty;
    public string? ProviderRef { get; private set; }
    public string? PdfUrl { get; private set; }
    public string? HtmlUrl { get; private set; }
    public string? XmlContent { get; private set; }
    public int CreditUsed { get; private set; }

    // Baglanti
    public Guid? OriginalInvoiceId { get; private set; }
    public Guid? OrderId { get; private set; }

    // Navigation
    public ICollection<EInvoiceLine> Lines { get; private set; } = new List<EInvoiceLine>();
    public ICollection<EInvoiceSendLog> SendLogs { get; private set; } = new List<EInvoiceSendLog>();

    public static EInvoiceDocument Create(
        string gibUuid, string ettnNo,
        EInvoiceScenario scenario, EInvoiceType type,
        DateTime issueDate, string sellerVkn, string sellerTitle,
        string buyerTitle, string providerId, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(gibUuid) || !Guid.TryParse(gibUuid, out _))
            throw new DomainValidationException(nameof(gibUuid), "GIB UUID gecerli bir GUID formatinda olmali.");
        if (string.IsNullOrWhiteSpace(ettnNo))
            throw new DomainValidationException(nameof(ettnNo), "ETTN numarasi bos olamaz.");
        if (string.IsNullOrWhiteSpace(sellerVkn) || sellerVkn.Length is not (10 or 11))
            throw new DomainValidationException(nameof(sellerVkn), "Satici VKN/TCKN 10 veya 11 haneli olmali.");

        var doc = new EInvoiceDocument
        {
            GibUuid = gibUuid, EttnNo = ettnNo, Scenario = scenario, Type = type,
            IssueDate = issueDate, DueDate = issueDate.AddDays(30),
            SellerVkn = sellerVkn, SellerTitle = sellerTitle, BuyerTitle = buyerTitle,
            ProviderId = providerId, Status = EInvoiceStatus.Draft, CreatedBy = createdBy
        };
        doc.RaiseDomainEvent(new EInvoiceCreatedEvent(doc.Id, ettnNo, type, DateTime.UtcNow));
        return doc;
    }

    public void MarkAsSent(string? providerRef, int creditUsed)
    {
        if (Status == EInvoiceStatus.Cancelled)
            throw new BusinessRuleException("EInvoice.Send", "Iptal edilmis fatura gonderilemez.");
        Status = EInvoiceStatus.Sent;
        ProviderRef = providerRef;
        CreditUsed = creditUsed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new EInvoiceSentEvent(Id, EttnNo, ProviderRef, DateTime.UtcNow));
    }

    public void SetPdfUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainValidationException(nameof(url), "PDF URL bos olamaz.");
        PdfUrl = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == EInvoiceStatus.Cancelled)
            throw new BusinessRuleException("EInvoice.Cancel", "Fatura zaten iptal edilmis.");
        if (Status == EInvoiceStatus.Accepted)
            throw new BusinessRuleException("EInvoice.Cancel", "Kabul edilmis fatura iptal edilemez.");
        Status = EInvoiceStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = cancelledBy;
        RaiseDomainEvent(new EInvoiceCancelledEvent(Id, EttnNo, reason, DateTime.UtcNow));
    }

    public void AddLine(EInvoiceLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        Lines.Add(line);
    }

    /// <summary>
    /// KDV tevkifat bilgilerini ayarlar.
    /// WithholdingAmount = TaxAmount * rate olarak hesaplanir.
    /// </summary>
    /// <param name="rate">Tevkifat orani (0-1 arasi, ornegin 0.50 = 5/10).</param>
    public void SetWithholding(decimal rate)
    {
        if (rate < 0 || rate > 1)
            throw new DomainValidationException(nameof(rate), "Tevkifat orani 0 ile 1 arasinda olmalidir.");
        WithholdingRate = rate;
        WithholdingAmount = Math.Round(TaxAmount * rate, 2);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFinancials(decimal lineExtension, decimal taxExclusive, decimal taxInclusive,
        decimal allowance, decimal taxAmount, decimal payable, string currency = "TRY")
    {
        if (payable < 0)
            throw new DomainValidationException(nameof(payable), "Odenecek tutar negatif olamaz.");
        LineExtensionAmount = lineExtension;
        TaxExclusiveAmount = taxExclusive;
        TaxInclusiveAmount = taxInclusive;
        AllowanceTotalAmount = allowance;
        TaxAmount = taxAmount;
        PayableAmount = payable;
        CurrencyCode = currency;
    }
}
