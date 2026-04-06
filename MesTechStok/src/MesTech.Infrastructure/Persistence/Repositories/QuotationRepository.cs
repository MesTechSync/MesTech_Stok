using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class QuotationRepository : IQuotationRepository
{
    private readonly AppDbContext _context;

    public QuotationRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Quotation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Quotations.FirstOrDefaultAsync(q => q.Id == id, ct).ConfigureAwait(false);

    public async Task<Quotation?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default)
        => await _context.Quotations
            .Include(q => q.Lines)
            .AsNoTracking().FirstOrDefaultAsync(q => q.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Quotation>> GetAllAsync(CancellationToken ct = default)
        => await _context.Quotations
            .OrderByDescending(q => q.QuotationDate)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Quotation>> GetByStatusAsync(QuotationStatus status, CancellationToken ct = default)
        => await _context.Quotations
            .Where(q => q.Status == status)
            .OrderByDescending(q => q.QuotationDate)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Quotation quotation, CancellationToken ct = default)
        => await _context.Quotations.AddAsync(quotation, ct).ConfigureAwait(false);

    public Task UpdateAsync(Quotation quotation, CancellationToken ct = default)
    {
        _context.Quotations.Update(quotation);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var quotation = await _context.Quotations.FirstOrDefaultAsync(q => q.Id == id, ct).ConfigureAwait(false);
        if (quotation != null) _context.Quotations.Remove(quotation);
    }
}
