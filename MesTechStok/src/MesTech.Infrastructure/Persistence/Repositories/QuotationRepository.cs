using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class QuotationRepository : IQuotationRepository
{
    private readonly AppDbContext _context;

    public QuotationRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Quotation?> GetByIdAsync(Guid id)
        => await _context.Quotations.FindAsync(id).ConfigureAwait(false);

    public async Task<Quotation?> GetByIdWithLinesAsync(Guid id)
        => await _context.Quotations
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<Quotation>> GetAllAsync()
        => await _context.Quotations
            .OrderByDescending(q => q.QuotationDate)
            .ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Quotation>> GetByStatusAsync(QuotationStatus status)
        => await _context.Quotations
            .Where(q => q.Status == status)
            .OrderByDescending(q => q.QuotationDate)
            .ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(Quotation quotation)
        => await _context.Quotations.AddAsync(quotation).ConfigureAwait(false);

    public Task UpdateAsync(Quotation quotation)
    {
        _context.Quotations.Update(quotation);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var quotation = await _context.Quotations.FindAsync(id).ConfigureAwait(false);
        if (quotation != null) _context.Quotations.Remove(quotation);
    }
}
