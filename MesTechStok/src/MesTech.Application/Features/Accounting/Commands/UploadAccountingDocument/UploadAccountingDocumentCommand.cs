using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;

public record UploadAccountingDocumentCommand(
    Guid TenantId,
    string FileName,
    string MimeType,
    long FileSize,
    string StoragePath,
    DocumentType DocumentType,
    DocumentSource DocumentSource,
    Guid? CounterpartyId = null,
    decimal? Amount = null,
    string? ExtractedData = null
) : IRequest<Guid>;
