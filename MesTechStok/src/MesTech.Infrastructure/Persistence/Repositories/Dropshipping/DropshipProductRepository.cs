using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Dropshipping;

public sealed class DropshipProductRepository : IDropshipProductRepository
{
    private readonly AppDbContext _context;

    public DropshipProductRepository(AppDbContext context) => _context = context;

    public async Task<DropshipProduct?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DropshipProducts
            .AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<DropshipProduct>> GetByTenantAsync(
        Guid tenantId,
        bool? isLinked = null,
        CancellationToken ct = default)
    {
        var query = _context.DropshipProducts
            .Where(p => p.TenantId == tenantId);

        if (isLinked.HasValue)
            query = query.Where(p => p.IsLinked == isLinked.Value);

        return await query.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DropshipProduct>> GetBySupplierAsync(
        Guid supplierId,
        CancellationToken ct = default)
        => await _context.DropshipProducts
            .Where(p => p.DropshipSupplierId == supplierId)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(DropshipProduct product, CancellationToken ct = default)
        => await _context.DropshipProducts.AddAsync(product, ct);

    public async Task AddRangeAsync(IEnumerable<DropshipProduct> products, CancellationToken ct = default)
        => await _context.DropshipProducts.AddRangeAsync(products, ct);

    public Task UpdateAsync(DropshipProduct product, CancellationToken ct = default)
    {
        _context.DropshipProducts.Update(product);
        return Task.CompletedTask;
    }
}
