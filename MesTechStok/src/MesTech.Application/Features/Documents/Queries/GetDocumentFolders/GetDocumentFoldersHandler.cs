using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Documents.Queries.GetDocumentFolders;

public sealed class GetDocumentFoldersHandler : IRequestHandler<GetDocumentFoldersQuery, DocumentFoldersResult>
{
    private readonly IDocumentFolderRepository _folderRepo;
    private readonly IDocumentRepository _docRepo;
    private readonly ILogger<GetDocumentFoldersHandler> _logger;

    public GetDocumentFoldersHandler(
        IDocumentFolderRepository folderRepo,
        IDocumentRepository docRepo,
        ILogger<GetDocumentFoldersHandler> logger)
    {
        _folderRepo = folderRepo ?? throw new ArgumentNullException(nameof(folderRepo));
        _docRepo = docRepo ?? throw new ArgumentNullException(nameof(docRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentFoldersResult> Handle(GetDocumentFoldersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching document folders for tenant {TenantId}", request.TenantId);

        var folders = await _folderRepo.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        // Batch count — tek SQL ile tüm folder'ların doküman sayısı (N+1 → 1+1)
        var folderIds = folders.Select(f => f.Id).ToList();
        var countsByFolder = await _docRepo.CountByFolderIdsAsync(folderIds, cancellationToken).ConfigureAwait(false);

        var folderDtos = new List<DocumentFolderDto>(folders.Count);
        foreach (var folder in folders)
        {
            folderDtos.Add(new DocumentFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentId = folder.ParentFolderId,
                DocumentCount = countsByFolder.GetValueOrDefault(folder.Id, 0),
                CreatedAt = folder.CreatedAt
            });
        }

        return new DocumentFoldersResult
        {
            Folders = folderDtos,
            TotalCount = folderDtos.Count
        };
    }
}
