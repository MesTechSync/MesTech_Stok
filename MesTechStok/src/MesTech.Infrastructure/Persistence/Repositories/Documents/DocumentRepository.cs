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
        => await _context.Documents.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Document>> GetByFolderAsync(Guid folderId, CancellationToken ct = default)
        => await _context.Documents.Where(d => d.FolderId == folderId)
                         .OrderByDescending(d => d.CreatedAt).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Document>> GetByOrderAsync(Guid orderId, CancellationToken ct = default)
        => await _context.Documents.Where(d => d.OrderId == orderId).AsNoTracking().ToListAsync(ct);

    public async Task<long> GetTotalSizeBytesAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Documents.Where(d => d.TenantId == tenantId).SumAsync(d => d.FileSizeBytes, ct);

    public async Task AddAsync(Document document, CancellationToken ct = default)
        => await _context.Documents.AddAsync(document, ct);
}
