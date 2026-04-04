using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByDateRangeWithItemsAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetStaleOrdersAsync(
        Guid tenantId, DateTime cutoffDate, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetRecentAsync(Guid tenantId, int count, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}
