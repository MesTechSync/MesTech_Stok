using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Persistence.Repositories.Documents;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;
    public DocumentRepository(AppDbContext context) => _context = context;

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Documents.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Document>> GetByFolderAsync(Guid folderId, CancellationToken ct = default)
        => await _context.Documents.Where(d => d.FolderId == folderId)
                         .OrderByDescending(d => d.CreatedAt).Take(1000) // G485: pagination guard
                         .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Document>> GetByOrderAsync(Guid orderId, CancellationToken ct = default)
        => await _context.Documents.Where(d => d.OrderId == orderId).Take(1000) // G485: pagination guard
                         .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<long> GetTotalSizeBytesAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Documents.Where(d => d.TenantId == tenantId).SumAsync(d => d.FileSizeBytes, ct).ConfigureAwait(false);

    public async Task AddAsync(Document document, CancellationToken ct = default)
        => await _context.Documents.AddAsync(document, ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, int>> CountByFolderIdsAsync(
        IEnumerable<Guid> folderIds, CancellationToken ct = default)
    {
        var idList = folderIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, int>();

        return await _context.Documents
            .Where(d => d.FolderId.HasValue && idList.Contains(d.FolderId.Value))
            .GroupBy(d => d.FolderId!.Value)
            .Select(g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FolderId, x => x.Count, ct)
            .ConfigureAwait(false);
    }
}
