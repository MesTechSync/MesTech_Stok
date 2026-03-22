using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.EInvoice;

public class EInvoiceSendLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EInvoiceDocumentId { get; private set; }
    public string ProviderId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ProviderRef { get; private set; }
    public int HttpStatusCode { get; private set; }
    public DateTime AttemptedAt { get; private set; } = DateTime.UtcNow;
}
