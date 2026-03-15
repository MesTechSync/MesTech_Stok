using MesTech.Domain.Entities.Documents;

namespace MesTech.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByFolderAsync(Guid folderId, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<long> GetTotalSizeBytesAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
}
