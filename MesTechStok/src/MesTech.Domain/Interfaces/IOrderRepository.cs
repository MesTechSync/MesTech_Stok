using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task<int> GetCountAsync();
}
