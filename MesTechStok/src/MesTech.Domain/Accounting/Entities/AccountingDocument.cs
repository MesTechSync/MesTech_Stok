using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Muhasebe belgesi — fatura, fis, ekstre vb. dosya meta verisi.
/// Dosya icerigi MinIO/S3 gibi object storage'da saklanir.
/// </summary>
public sealed class AccountingDocument : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public DocumentSource DocumentSource { get; private set; }
    public Guid? CounterpartyId { get; private set; }
    public decimal? Amount { get; private set; }
    public string? ExtractedData { get; private set; }
    public byte[]? RowVersion { get; set; }

    // Navigation
    public Counterparty? Counterparty { get; private set; }

    private AccountingDocument() { }

    public static AccountingDocument Create(
        Guid tenantId,
        string fileName,
        string mimeType,
        long fileSize,
        string storagePath,
        DocumentType documentType,
        DocumentSource documentSource,
        Guid? counterpartyId = null,
        decimal? amount = null,
        string? extractedData = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var doc = new AccountingDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FileName = fileName,
            MimeType = mimeType,
            FileSize = fileSize,
            StoragePath = storagePath,
            DocumentType = documentType,
            DocumentSource = documentSource,
            CounterpartyId = counterpartyId,
            Amount = amount,
            ExtractedData = extractedData,
            CreatedAt = DateTime.UtcNow
        };

        doc.RaiseDomainEvent(new DocumentReceivedEvent
        {
            TenantId = tenantId,
            DocumentId = doc.Id,
            FileName = fileName,
            DocumentType = documentType,
            Source = documentSource
        });

        return doc;
    }

    public void UpdateExtractedData(string extractedData)
    {
        ExtractedData = extractedData;
        UpdatedAt = DateTime.UtcNow;
    }
}
