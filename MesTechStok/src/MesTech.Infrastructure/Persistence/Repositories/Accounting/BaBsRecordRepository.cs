using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Accounting;

public class BaBsRecordRepository : IBaBsRecordRepository
{
    private readonly AppDbContext _context;

    public BaBsRecordRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<BaBsRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.BaBsRecords
            .AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<BaBsRecord>> GetAllAsync(Guid tenantId, int? year = null, int? month = null, CancellationToken ct = default)
        => await _context.BaBsRecords
            .Where(r => r.TenantId == tenantId)
            .Where(r => year == null || r.Year == year.Value)
            .Where(r => month == null || r.Month == month.Value)
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .ThenBy(r => r.CounterpartyName)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(BaBsRecord record, CancellationToken ct = default)
        => await _context.BaBsRecords.AddAsync(record, ct).ConfigureAwait(false);
}
