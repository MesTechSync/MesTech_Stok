using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class TaxWithholdingRepository : ITaxWithholdingRepository
{
    private readonly AppDbContext _context;
    public TaxWithholdingRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<TaxWithholding>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default)
        => await _context.TaxWithholdings
            .Where(w => w.InvoiceId == invoiceId)
            .OrderByDescending(w => w.CreatedAt)
            .AsNoTracking().ToListAsync(ct);

    public async Task<decimal> GetTotalWithholdingAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.TaxWithholdings
            .Where(w => w.TenantId == tenantId && w.CreatedAt >= from && w.CreatedAt <= to)
            .SumAsync(w => w.WithholdingAmount, ct);

    public async Task AddAsync(TaxWithholding withholding, CancellationToken ct = default)
        => await _context.TaxWithholdings.AddAsync(withholding, ct);
}
