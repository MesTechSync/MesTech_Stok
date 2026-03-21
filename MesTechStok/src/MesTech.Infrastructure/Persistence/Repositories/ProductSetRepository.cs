using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class ProductSetRepository : IProductSetRepository
{
    private readonly AppDbContext _context;

    public ProductSetRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ProductSet?> GetByIdAsync(Guid id)
        => await _context.ProductSets
            .Include(ps => ps.Items)
            .AsNoTracking().FirstOrDefaultAsync(ps => ps.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductSet>> GetAllAsync(Guid? tenantId = null)
        => await _context.ProductSets
            .Include(ps => ps.Items)
            .Where(ps => tenantId == null || ps.TenantId == tenantId.Value)
            .OrderBy(ps => ps.Name)
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(ProductSet productSet)
        => await _context.ProductSets.AddAsync(productSet).ConfigureAwait(false);

    public Task UpdateAsync(ProductSet productSet)
    {
        _context.ProductSets.Update(productSet);
        return Task.CompletedTask;
    }
}
