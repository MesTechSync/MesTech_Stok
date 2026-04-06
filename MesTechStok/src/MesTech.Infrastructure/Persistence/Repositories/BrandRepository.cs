using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _context;

    public BrandRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Brands.FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default)
        => await _context.Brands
            .OrderBy(b => b.Name)
            .Take(5000)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<Brand?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(b => EF.Functions.ILike(b.Name, name), ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Brand brand, CancellationToken ct = default)
        => await _context.Brands.AddAsync(brand, ct).ConfigureAwait(false);

    public Task UpdateAsync(Brand brand, CancellationToken ct = default)
    {
        _context.Brands.Update(brand);
        return Task.CompletedTask;
    }
}
