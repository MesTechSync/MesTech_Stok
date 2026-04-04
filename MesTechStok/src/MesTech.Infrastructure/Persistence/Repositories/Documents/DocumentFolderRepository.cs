using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Persistence.Repositories.Documents;

public sealed class DocumentFolderRepository : IDocumentFolderRepository
{
    private readonly AppDbContext _context;
    public DocumentFolderRepository(AppDbContext context) => _context = context;

    public async Task<DocumentFolder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DocumentFolders.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<DocumentFolder>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.DocumentFolders.Where(f => f.TenantId == tenantId)
                         .OrderBy(f => f.Position).Take(1000) // G485: pagination guard
                         .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<DocumentFolder>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
        => await _context.DocumentFolders.Where(f => f.ParentFolderId == parentId)
                         .OrderBy(f => f.Position).Take(1000) // G485: pagination guard
                         .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(DocumentFolder folder, CancellationToken ct = default)
        => await _context.DocumentFolders.AddAsync(folder, ct).ConfigureAwait(false);
}
