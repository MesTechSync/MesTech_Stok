using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class AccountingDocumentRepository : IAccountingDocumentRepository
{
    private readonly AppDbContext _context;
    public AccountingDocumentRepository(AppDbContext context) => _context = context;

    public async Task<AccountingDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AccountingDocuments.FindAsync([id], ct);

    public async Task<IReadOnlyList<AccountingDocument>> GetByTypeAsync(Guid tenantId, DocumentType type, CancellationToken ct = default)
        => await _context.AccountingDocuments
            .Where(d => d.TenantId == tenantId && d.DocumentType == type)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<AccountingDocument>> GetByCounterpartyAsync(Guid tenantId, Guid counterpartyId, CancellationToken ct = default)
        => await _context.AccountingDocuments
            .Where(d => d.TenantId == tenantId && d.CounterpartyId == counterpartyId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(AccountingDocument document, CancellationToken ct = default)
        => await _context.AccountingDocuments.AddAsync(document, ct);

    public Task UpdateAsync(AccountingDocument document, CancellationToken ct = default)
    {
        _context.AccountingDocuments.Update(document);
        return Task.CompletedTask;
    }
}
