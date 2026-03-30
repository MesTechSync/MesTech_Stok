using MediatR;

namespace MesTech.Application.Features.Documents.Queries.GetDocumentFolders;

public record GetDocumentFoldersQuery(Guid TenantId) : IRequest<DocumentFoldersResult>;

public sealed class DocumentFoldersResult
{
    public IReadOnlyList<DocumentFolderDto> Folders { get; init; } = [];
    public int TotalCount { get; init; }
}
