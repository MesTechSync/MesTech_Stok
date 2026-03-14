using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Documents;

namespace MesTech.Domain.Entities.Documents;

public class Document : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? FolderId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DocumentVisibility Visibility { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? Tags { get; private set; }

    private Document() { }

    public static Document Create(
        Guid tenantId, string fileName, string originalFileName, string contentType,
        long fileSizeBytes, string storagePath, Guid uploadedByUserId,
        DocumentVisibility visibility = DocumentVisibility.TenantOnly,
        Guid? folderId = null, string? description = null,
        Guid? orderId = null, Guid? invoiceId = null, Guid? productId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FolderId = folderId,
            FileName = fileName,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath,
            UploadedByUserId = uploadedByUserId,
            Visibility = visibility,
            Description = description,
            OrderId = orderId,
            InvoiceId = invoiceId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };
        doc.RaiseDomainEvent(new DocumentUploadedEvent(doc.Id, fileName, tenantId, DateTime.UtcNow));
        return doc;
    }
}
