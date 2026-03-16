using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Order?> GetByIdAsync(Guid id)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id).ConfigureAwait(false);

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId)
        => await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to)
        => await _context.Orders
            .Where(o => o.OrderDate >= from && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.TenantId == tenantId
                     && o.OrderDate >= from
                     && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> GetByDateRangeWithItemsAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.TenantId == tenantId
                     && o.OrderDate >= from
                     && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Order order)
        => await _context.Orders.AddAsync(order).ConfigureAwait(false);

    public Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public async Task<int> GetCountAsync()
        => await _context.Orders.CountAsync().ConfigureAwait(false);
}
