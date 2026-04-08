using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class CariHareketRepository : ICariHareketRepository
{
    private readonly AppDbContext _context;

    public CariHareketRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<CariHareket?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CariHareketler.FirstOrDefaultAsync(h => h.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHareket>> GetByCariHesapIdAsync(Guid cariHesapId, CancellationToken ct = default)
        => await _context.CariHareketler
            .Where(h => h.CariHesapId == cariHesapId)
            .OrderByDescending(h => h.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHareket>> GetByDateRangeAsync(Guid cariHesapId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.CariHareketler
            .Where(h => h.CariHesapId == cariHesapId && h.Date >= from && h.Date <= to)
            .OrderByDescending(h => h.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(CariHareket hareket, CancellationToken ct = default)
        => await _context.CariHareketler.AddAsync(hareket, ct).ConfigureAwait(false);
}
