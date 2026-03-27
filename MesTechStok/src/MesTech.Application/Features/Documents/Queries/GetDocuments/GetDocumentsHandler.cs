using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Documents.Queries.GetDocuments;

public sealed class GetDocumentsHandler : IRequestHandler<GetDocumentsQuery, DocumentsResult>
{
    private readonly IDocumentRepository _docRepo;

    public GetDocumentsHandler(IDocumentRepository docRepo)
        => _docRepo = docRepo ?? throw new ArgumentNullException(nameof(docRepo));

    public async Task<DocumentsResult> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (!request.FolderId.HasValue)
            return new DocumentsResult { TotalCount = 0 };

        var docs = await _docRepo.GetByFolderAsync(request.FolderId.Value, cancellationToken).ConfigureAwait(false);

        var paged = docs
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DocumentListDto
            {
                Id = d.Id,
                FileName = d.FileName,
                MimeType = d.ContentType,
                SizeBytes = d.FileSizeBytes,
                FolderId = d.FolderId,
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy
            }).ToList();

        return new DocumentsResult { Documents = paged, TotalCount = docs.Count };
    }
}
