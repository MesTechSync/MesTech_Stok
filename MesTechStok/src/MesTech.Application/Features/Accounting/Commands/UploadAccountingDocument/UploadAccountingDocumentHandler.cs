using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;

public class UploadAccountingDocumentHandler : IRequestHandler<UploadAccountingDocumentCommand, Guid>
{
    private readonly IAccountingDocumentRepository _repository;
    private readonly IUnitOfWork _uow;

    public UploadAccountingDocumentHandler(IAccountingDocumentRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(UploadAccountingDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = AccountingDocument.Create(
            request.TenantId, request.FileName, request.MimeType, request.FileSize,
            request.StoragePath, request.DocumentType, request.DocumentSource,
            request.CounterpartyId, request.Amount, request.ExtractedData);

        await _repository.AddAsync(document, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return document.Id;
    }
}
