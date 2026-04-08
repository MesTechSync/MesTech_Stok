using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductSpecificationRepository : IProductSpecificationRepository
{
    private readonly AppDbContext _context;
    public ProductSpecificationRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductSpecification>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _context.ProductSpecifications
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .Take(200)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(ProductSpecification spec, CancellationToken ct = default)
        => await _context.ProductSpecifications.AddAsync(spec, ct).ConfigureAwait(false);

    public Task UpdateAsync(ProductSpecification spec, CancellationToken ct = default)
    {
        _context.ProductSpecifications.Update(spec);
        return Task.CompletedTask;
    }
}
