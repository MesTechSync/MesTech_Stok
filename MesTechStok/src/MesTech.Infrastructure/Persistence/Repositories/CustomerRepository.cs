using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
        => await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
        => await _context.Customers.OrderBy(c => c.Name).Take(5000).AsNoTracking().ToListAsync(); // G485

    public async Task<IReadOnlyList<Customer>> GetActiveAsync()
        => await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync();

    public async Task AddAsync(Customer customer)
        => await _context.Customers.AddAsync(customer);

    public Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        // FindAsync bypasses global query filter (tenant isolation) — use FirstOrDefaultAsync
        var entity = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is not null)
            _context.Customers.Remove(entity);
    }
}
