using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Documents.Queries.GetDocuments;

public record GetDocumentsQuery(Guid TenantId, Guid? FolderId = null, int Page = 1, int PageSize = 20)
    : IRequest<DocumentsResult>, ICacheableQuery
{
    public string CacheKey => $"Documents_{TenantId}_{FolderId}_{Page}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public sealed class DocumentsResult
{
    public IReadOnlyList<DocumentListDto> Documents { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class DocumentListDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string? MimeType { get; init; }
    public long SizeBytes { get; init; }
    public Guid? FolderId { get; init; }
    public string? FolderName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}
