using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Documents.Commands.UploadDocument;

/// <summary>
/// Belge yukleme isleyicisi — MinIO'ya yukle + Document entity kaydet.
/// G417: DocumentManager handler.
/// </summary>
public sealed class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResult>
{
    private readonly IDocumentStorageService _storage;
    private readonly IDocumentRepository _docRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadDocumentHandler> _logger;

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public UploadDocumentHandler(
        IDocumentStorageService storage,
        IDocumentRepository docRepo,
        IUnitOfWork unitOfWork,
        ILogger<UploadDocumentHandler> logger)
    {
        _storage = storage;
        _docRepo = docRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UploadDocumentResult> Handle(
        UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FileSizeBytes <= 0)
            return UploadDocumentResult.Failure("Dosya boyutu gecersiz.");

        if (request.FileSizeBytes > MaxFileSizeBytes)
            return UploadDocumentResult.Failure($"Dosya boyutu limiti asildi ({MaxFileSizeBytes / 1024 / 1024} MB).");

        if (string.IsNullOrWhiteSpace(request.FileName))
            return UploadDocumentResult.Failure("Dosya adi bos olamaz.");

        try
        {
            var storagePath = await _storage.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                cancellationToken: cancellationToken);

            var document = Document.Create(
                request.TenantId,
                request.FileName,
                request.FileName,
                request.ContentType,
                request.FileSizeBytes,
                storagePath,
                request.UserId,
                request.FolderId,
                DocumentVisibility.TenantOnly,
                request.Description);

            if (request.OrderId.HasValue) document.LinkToOrder(request.OrderId.Value);
            if (request.InvoiceId.HasValue) document.LinkToInvoice(request.InvoiceId.Value);
            if (request.ProductId.HasValue) document.LinkToProduct(request.ProductId.Value);

            await _docRepo.AddAsync(document, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Document uploaded: {DocId} — {FileName}", document.Id, request.FileName);

            return UploadDocumentResult.Success(document.Id, storagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload failed: {FileName}", request.FileName);
            return UploadDocumentResult.Failure($"Yukleme hatasi: {ex.Message}");
        }
    }
}
