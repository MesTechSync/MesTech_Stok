using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Dropshipping;

public sealed class DropshipOrderRepository : IDropshipOrderRepository
{
    private readonly AppDbContext _context;

    public DropshipOrderRepository(AppDbContext context) => _context = context;

    public async Task<DropshipOrder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DropshipOrders
            .AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<DropshipOrder>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken ct = default)
        => await _context.DropshipOrders
            .Where(o => o.TenantId == tenantId)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<DropshipOrder?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => await _context.DropshipOrders
            .AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId, ct).ConfigureAwait(false);

    public async Task AddAsync(DropshipOrder order, CancellationToken ct = default)
        => await _context.DropshipOrders.AddAsync(order, ct).ConfigureAwait(false);

    public Task UpdateAsync(DropshipOrder order, CancellationToken ct = default)
    {
        _context.DropshipOrders.Update(order);
        return Task.CompletedTask;
    }
}
