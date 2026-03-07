using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task<int> GetCountAsync();
}
