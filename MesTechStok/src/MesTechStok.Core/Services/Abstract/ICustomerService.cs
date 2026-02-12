using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

public interface ICustomerService
{
    Task<PagedResult<Customer>> GetCustomersPagedAsync(int page, int pageSize, string? searchTerm = null);
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(int id);
    Task<CustomerStatistics> GetStatisticsAsync();
}


