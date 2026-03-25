using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class BillingInvoiceRepository : IBillingInvoiceRepository
{
    private readonly AppDbContext _context;

    public BillingInvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BillingInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.BillingInvoices
            .Include(i => i.Subscription)
            .AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<BillingInvoice>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.BillingInvoices
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssueDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<BillingInvoice>> GetOverdueAsync(CancellationToken ct = default)
        => await _context.BillingInvoices
            .Where(i => i.Status == BillingInvoiceStatus.Sent && i.DueDate < DateTime.UtcNow)
            .AsNoTracking().ToListAsync(ct);

    public async Task<int> GetNextSequenceAsync(CancellationToken ct = default)
    {
        var count = await _context.BillingInvoices.CountAsync(ct);
        return count + 1;
    }

    public async Task AddAsync(BillingInvoice invoice, CancellationToken ct = default)
        => await _context.BillingInvoices.AddAsync(invoice, ct);

    public Task UpdateAsync(BillingInvoice invoice, CancellationToken ct = default)
    {
        _context.BillingInvoices.Update(invoice);
        return Task.CompletedTask;
    }
}
