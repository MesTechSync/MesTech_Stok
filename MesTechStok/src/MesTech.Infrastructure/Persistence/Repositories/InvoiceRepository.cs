using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Invoice?> GetByIdAsync(Guid id)
        => await _context.Invoices.FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);

    public async Task<Invoice?> GetByOrderIdAsync(Guid orderId)
        => await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.OrderId == orderId).ConfigureAwait(false);

    public async Task<IReadOnlyList<Invoice>> GetFailedAsync(int maxCount, CancellationToken ct = default)
        => await _context.Invoices
            .Where(i => i.Status == Domain.Enums.InvoiceStatus.Error || i.ParasutSyncStatus == Domain.Enums.SyncStatus.Failed)
            .OrderBy(i => i.CreatedAt)
            .Take(maxCount)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Invoice invoice)
        => await _context.Invoices.AddAsync(invoice).ConfigureAwait(false);

    public Task UpdateAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        return Task.CompletedTask;
    }
}
