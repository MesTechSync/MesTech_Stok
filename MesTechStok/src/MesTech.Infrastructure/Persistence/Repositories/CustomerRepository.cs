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

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
        => await _context.Customers.OrderBy(c => c.Name).Take(5000).AsNoTracking().ToListAsync(ct).ConfigureAwait(false); // G485

    public async Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default)
        => await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
        => await _context.Customers.AddAsync(customer, ct).ConfigureAwait(false);

    public Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // FindAsync bypasses global query filter (tenant isolation) — use FirstOrDefaultAsync
        var entity = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (entity is not null)
            _context.Customers.Remove(entity);
    }
}
