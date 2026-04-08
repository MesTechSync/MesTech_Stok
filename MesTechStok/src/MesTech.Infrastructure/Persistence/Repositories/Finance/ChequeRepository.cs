using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Finance;

public sealed class ChequeRepository : IChequeRepository
{
    private readonly AppDbContext _context;
    public ChequeRepository(AppDbContext context) => _context = context;

    public async Task<Cheque?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Cheque>> GetByTenantAsync(
        Guid tenantId, ChequeStatus? status = null, CancellationToken ct = default)
    {
        var q = _context.Cheques.Where(c => c.TenantId == tenantId);
        if (status.HasValue) q = q.Where(c => c.Status == status.Value);
        return await q.OrderBy(c => c.MaturityDate).Take(1000)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Cheque>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Cheques
            .Where(c => c.TenantId == tenantId
                     && c.Status == ChequeStatus.InPortfolio
                     && c.MaturityDate < DateTime.UtcNow)
            .OrderBy(c => c.MaturityDate)
            .Take(500)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Cheque cheque, CancellationToken ct = default)
        => await _context.Cheques.AddAsync(cheque, ct).ConfigureAwait(false);

    public Task UpdateAsync(Cheque cheque, CancellationToken ct = default)
    {
        _context.Cheques.Update(cheque);
        return Task.CompletedTask;
    }
}
