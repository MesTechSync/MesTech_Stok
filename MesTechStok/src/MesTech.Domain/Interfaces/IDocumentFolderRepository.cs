using MesTech.Domain.Entities.Documents;

namespace MesTech.Domain.Interfaces;

public interface IDocumentFolderRepository
{
    Task<DocumentFolder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentFolder>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentFolder>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);
    Task AddAsync(DocumentFolder folder, CancellationToken ct = default);
}
