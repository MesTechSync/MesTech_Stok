using MediatR;

namespace MesTech.Application.Features.Documents.Commands.UploadDocument;

/// <summary>
/// Belge yukleme komutu — MinIO'ya dosya yukler, Document entity olusturur.
/// G417: DocumentManager handler.
/// </summary>
public record UploadDocumentCommand(
    Guid TenantId,
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream,
    Guid? FolderId = null,
    string? Description = null,
    Guid? OrderId = null,
    Guid? InvoiceId = null,
    Guid? ProductId = null) : IRequest<UploadDocumentResult>;

public sealed class UploadDocumentResult
{
    public bool IsSuccess { get; init; }
    public Guid DocumentId { get; init; }
    public string StoragePath { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static UploadDocumentResult Success(Guid id, string path)
        => new() { IsSuccess = true, DocumentId = id, StoragePath = path };

    public static UploadDocumentResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
