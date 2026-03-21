using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class CariHareketRepository : ICariHareketRepository
{
    private readonly AppDbContext _context;

    public CariHareketRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<CariHareket>> GetByCariHesapIdAsync(Guid cariHesapId)
        => await _context.CariHareketler
            .Where(h => h.CariHesapId == cariHesapId)
            .OrderByDescending(h => h.Date)
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<CariHareket>> GetByDateRangeAsync(Guid cariHesapId, DateTime from, DateTime to)
        => await _context.CariHareketler
            .Where(h => h.CariHesapId == cariHesapId && h.Date >= from && h.Date <= to)
            .OrderByDescending(h => h.Date)
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(CariHareket hareket)
        => await _context.CariHareketler.AddAsync(hareket).ConfigureAwait(false);
}
