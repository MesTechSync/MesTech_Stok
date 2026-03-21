using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class EInvoiceDocumentRepository : IEInvoiceDocumentRepository
{
    private readonly AppDbContext _context;
    public EInvoiceDocumentRepository(AppDbContext context) => _context = context;

    public async Task<EInvoiceDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.EInvoiceDocuments
            .Include(d => d.Lines)
            .Include(d => d.SendLogs)
            .AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<EInvoiceDocument?> GetByEttnNoAsync(string ettnNo, CancellationToken ct = default)
        => await _context.EInvoiceDocuments
            .AsNoTracking().FirstOrDefaultAsync(d => d.EttnNo == ettnNo, ct);

    public async Task<EInvoiceDocument?> GetByGibUuidAsync(string gibUuid, CancellationToken ct = default)
        => await _context.EInvoiceDocuments
            .AsNoTracking().FirstOrDefaultAsync(d => d.GibUuid == gibUuid, ct);

    public async Task<(IReadOnlyList<EInvoiceDocument> Items, int Total)> GetPagedAsync(
        EInvoiceStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        IQueryable<EInvoiceDocument> query = _context.EInvoiceDocuments.AsNoTracking();

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (from.HasValue)
            query = query.Where(d => d.IssueDate >= from.Value);

        if (to.HasValue)
            query = query.Where(d => d.IssueDate <= to.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(EInvoiceDocument doc, CancellationToken ct = default)
        => await _context.EInvoiceDocuments.AddAsync(doc, ct);

    public Task UpdateAsync(EInvoiceDocument doc, CancellationToken ct = default)
    {
        _context.EInvoiceDocuments.Update(doc);
        return Task.CompletedTask;
    }
}
