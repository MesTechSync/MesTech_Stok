using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

public interface ICustomerService
{
    Task<PagedResult<Customer>> GetCustomersPagedAsync(int page, int pageSize, string? searchTerm = null);
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(Guid id);
    Task<CustomerStatistics> GetStatisticsAsync();
}


