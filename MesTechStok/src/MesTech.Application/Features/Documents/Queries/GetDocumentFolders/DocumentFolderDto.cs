namespace MesTech.Application.Features.Documents.Queries.GetDocumentFolders;

public sealed class DocumentFolderDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public int DocumentCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
