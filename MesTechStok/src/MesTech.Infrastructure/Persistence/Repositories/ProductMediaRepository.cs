using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductMediaRepository : IProductMediaRepository
{
    private readonly AppDbContext _context;
    public ProductMediaRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductMedia>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _context.Set<ProductMedia>()
            .Where(m => m.ProductId == productId)
            .OrderBy(m => m.SortOrder)
            .Take(100)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(ProductMedia media, CancellationToken ct = default)
        => await _context.Set<ProductMedia>().AddAsync(media, ct).ConfigureAwait(false);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Set<ProductMedia>().FirstOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);
        if (entity is not null)
            _context.Set<ProductMedia>().Remove(entity);
    }
}
