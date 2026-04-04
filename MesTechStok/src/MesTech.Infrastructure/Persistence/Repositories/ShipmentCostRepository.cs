using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ShipmentCostRepository : IShipmentCostRepository
{
    private readonly AppDbContext _context;

    public ShipmentCostRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<ShipmentCost>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.ShipmentCosts
            .Where(s => s.TenantId == tenantId && s.ShippedAt >= from && s.ShippedAt <= to)
            .OrderByDescending(s => s.ShippedAt)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ShipmentCost>> GetByOrderIdAsync(
        Guid orderId, CancellationToken ct = default)
        => await _context.ShipmentCosts
            .Where(s => s.OrderId == orderId)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(ShipmentCost cost, CancellationToken ct = default)
        => await _context.ShipmentCosts.AddAsync(cost, ct).ConfigureAwait(false);
}
