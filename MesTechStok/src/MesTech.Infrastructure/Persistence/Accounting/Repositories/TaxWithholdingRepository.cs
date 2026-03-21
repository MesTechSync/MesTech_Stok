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

    public async Task<IReadOnlyList<TaxWithholding>> GetAllAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        var query = _context.TaxWithholdings
            .Where(w => w.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(w => w.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .AsNoTracking().ToListAsync(ct);
    }
}
