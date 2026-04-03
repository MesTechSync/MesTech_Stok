using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Dropshipping;

public sealed class DropshipSupplierRepository : IDropshipSupplierRepository
{
    private readonly AppDbContext _context;

    public DropshipSupplierRepository(AppDbContext context) => _context = context;

    public async Task<DropshipSupplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DropshipSuppliers
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<DropshipSupplier>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken ct = default)
        => await _context.DropshipSuppliers
            .Where(s => s.TenantId == tenantId)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(DropshipSupplier supplier, CancellationToken ct = default)
        => await _context.DropshipSuppliers.AddAsync(supplier, ct).ConfigureAwait(false);

    public Task UpdateAsync(DropshipSupplier supplier, CancellationToken ct = default)
    {
        _context.DropshipSuppliers.Update(supplier);
        return Task.CompletedTask;
    }
}
