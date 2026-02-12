using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Core.Services.Concrete;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomerService>? _logger;
    private readonly IServiceScopeFactory? _scopeFactory;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public CustomerService(AppDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Threading-safe constructor
    public CustomerService(AppDbContext context, ILogger<CustomerService> logger, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<PagedResult<Customer>> GetCustomersPagedAsync(int page, int pageSize, string? searchTerm = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;

        var query = _context.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c =>
                c.Name.Contains(searchTerm) ||
                (c.Email != null && c.Email.Contains(searchTerm)) ||
                (c.Phone != null && c.Phone.Contains(searchTerm)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        // Basit benzersizlik kontrolleri
        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            var exists = await _context.Customers.AnyAsync(c => c.Email == customer.Email);
            if (exists) throw new InvalidOperationException($"Email zaten mevcut: {customer.Email}");
        }
        if (!string.IsNullOrWhiteSpace(customer.Phone))
        {
            var exists = await _context.Customers.AnyAsync(c => c.Phone == customer.Phone);
            if (exists) throw new InvalidOperationException($"Telefon zaten mevcut: {customer.Phone}");
        }
        customer.CreatedDate = DateTime.UtcNow;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        if (existing == null) throw new InvalidOperationException($"Müşteri bulunamadı: {customer.Id}");

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            var exists = await _context.Customers.AnyAsync(c => c.Email == customer.Email && c.Id != customer.Id);
            if (exists) throw new InvalidOperationException($"Email başka müşteride mevcut: {customer.Email}");
        }
        if (!string.IsNullOrWhiteSpace(customer.Phone))
        {
            var exists = await _context.Customers.AnyAsync(c => c.Phone == customer.Phone && c.Id != customer.Id);
            if (exists) throw new InvalidOperationException($"Telefon başka müşteride mevcut: {customer.Phone}");
        }

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.City = customer.City;
        existing.Country = customer.Country;
        existing.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null) return false;
        // Soft delete yerine pasif etme
        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CustomerStatistics> GetStatisticsAsync()
    {
        try
        {
            // Threading-safe: Her istatistik çağrısı için yeni DbContext
            if (_scopeFactory != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var total = await dbContext.Customers.AsNoTracking().CountAsync();
                var active = await dbContext.Customers.AsNoTracking().CountAsync(c => c.IsActive);
                var vip = await dbContext.Customers.AsNoTracking().CountAsync(c => c.IsVip);

                return new CustomerStatistics
                {
                    TotalCustomers = total,
                    ActiveCustomers = active,
                    VipCustomers = vip,
                    AverageOrderValue = 0m // Basit hesap
                };
            }
            else
            {
                // Fallback - mevcut context kullan ama NoTracking ile
                var total = await _context.Customers.AsNoTracking().CountAsync();
                var active = await _context.Customers.AsNoTracking().CountAsync(c => c.IsActive);
                var vip = await _context.Customers.AsNoTracking().CountAsync(c => c.IsVip);

                return new CustomerStatistics
                {
                    TotalCustomers = total,
                    ActiveCustomers = active,
                    VipCustomers = vip,
                    AverageOrderValue = 0m
                };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetStatisticsAsync error");
            // Hata durumunda boş istatistik döndür
            return new CustomerStatistics
            {
                TotalCustomers = 0,
                ActiveCustomers = 0,
                VipCustomers = 0,
                AverageOrderValue = 0m
            };
        }
    }
}


