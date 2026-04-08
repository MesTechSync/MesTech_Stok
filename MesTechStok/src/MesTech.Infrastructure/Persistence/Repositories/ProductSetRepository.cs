using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductSetRepository : IProductSetRepository
{
    private readonly AppDbContext _context;

    public ProductSetRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ProductSet?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ProductSets
            .Include(ps => ps.Items)
            .AsNoTracking().FirstOrDefaultAsync(ps => ps.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductSet>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default)
        => await _context.ProductSets
            .Include(ps => ps.Items)
            .Where(ps => tenantId == null || ps.TenantId == tenantId.Value)
            .OrderBy(ps => ps.Name)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(ProductSet productSet, CancellationToken ct = default)
        => await _context.ProductSets.AddAsync(productSet, ct).ConfigureAwait(false);

    public Task UpdateAsync(ProductSet productSet, CancellationToken ct = default)
    {
        _context.ProductSets.Update(productSet);
        return Task.CompletedTask;
    }
}
