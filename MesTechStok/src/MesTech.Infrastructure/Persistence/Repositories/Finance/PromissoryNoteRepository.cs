using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Finance;

public sealed class PromissoryNoteRepository : IPromissoryNoteRepository
{
    private readonly AppDbContext _context;
    public PromissoryNoteRepository(AppDbContext context) => _context = context;

    public async Task<PromissoryNote?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PromissoryNotes.FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<PromissoryNote>> GetByTenantAsync(
        Guid tenantId, NoteStatus? status = null, CancellationToken ct = default)
    {
        var q = _context.PromissoryNotes.Where(n => n.TenantId == tenantId);
        if (status.HasValue) q = q.Where(n => n.Status == status.Value);
        return await q.OrderBy(n => n.MaturityDate).Take(1000)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PromissoryNote>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.PromissoryNotes
            .Where(n => n.TenantId == tenantId
                     && n.Status == NoteStatus.InPortfolio
                     && n.MaturityDate < DateTime.UtcNow)
            .OrderBy(n => n.MaturityDate)
            .Take(500)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(PromissoryNote note, CancellationToken ct = default)
        => await _context.PromissoryNotes.AddAsync(note, ct).ConfigureAwait(false);

    public Task UpdateAsync(PromissoryNote note, CancellationToken ct = default)
    {
        _context.PromissoryNotes.Update(note);
        return Task.CompletedTask;
    }
}
