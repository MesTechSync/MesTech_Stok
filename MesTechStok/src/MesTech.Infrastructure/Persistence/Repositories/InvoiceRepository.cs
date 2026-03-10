using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Invoice?> GetByIdAsync(Guid id)
        => await _context.Invoices.FindAsync(id).ConfigureAwait(false);

    public async Task AddAsync(Invoice invoice)
        => await _context.Invoices.AddAsync(invoice).ConfigureAwait(false);

    public Task UpdateAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        return Task.CompletedTask;
    }
}
