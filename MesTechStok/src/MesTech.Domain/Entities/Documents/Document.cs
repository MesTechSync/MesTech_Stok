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

    public static Document Create(Guid tenantId, string fileName,
        string originalFileName, string contentType, long fileSizeBytes,
        string storagePath, Guid uploadedByUserId,
        Guid? folderId = null,
        DocumentVisibility visibility = DocumentVisibility.TenantOnly,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSizeBytes);
        var doc = new Document
        {
            Id = Guid.NewGuid(), TenantId = tenantId, FolderId = folderId,
            FileName = fileName, OriginalFileName = originalFileName,
            ContentType = contentType, FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath, UploadedByUserId = uploadedByUserId,
            Visibility = visibility, Description = description, CreatedAt = DateTime.UtcNow
        };
        doc.RaiseDomainEvent(new DocumentUploadedEvent(doc.Id, originalFileName, fileSizeBytes, tenantId, DateTime.UtcNow));
        return doc;
    }

    public void LinkToOrder(Guid orderId) { OrderId = orderId; UpdatedAt = DateTime.UtcNow; }
    public void LinkToInvoice(Guid invoiceId) { InvoiceId = invoiceId; UpdatedAt = DateTime.UtcNow; }
    public void LinkToProduct(Guid productId) { ProductId = productId; UpdatedAt = DateTime.UtcNow; }
    public void MoveToFolder(Guid folderId) { FolderId = folderId; UpdatedAt = DateTime.UtcNow; }
}
