using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => idList.Contains(o.Id))
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking().FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .Take(1000) // G485: pagination guard — unbounded query protection
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.OrderDate >= from && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .Take(5000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.TenantId == tenantId
                     && o.OrderDate >= from
                     && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .Take(5000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByDateRangeWithItemsAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.TenantId == tenantId
                     && o.OrderDate >= from
                     && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .Take(5000) // G485: pagination guard — includes OrderItems
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await _context.Orders.AddAsync(order, ct).ConfigureAwait(false);

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Order>> GetStaleOrdersAsync(
        Guid tenantId, DateTime cutoffDate, CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.TenantId == tenantId
                     && o.Status == Domain.Enums.OrderStatus.Confirmed
                     && o.OrderDate < cutoffDate
                     && o.ShippedAt == null)
            .OrderBy(o => o.OrderDate)
            .Take(1000) // G485: pagination guard — stale order batch limit
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Include(o => o.OrderItems)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<int> GetCountAsync(CancellationToken ct = default)
        => await _context.Orders.CountAsync(ct).ConfigureAwait(false);
}
