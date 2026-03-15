using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface IAccountingDocumentRepository
{
    Task<AccountingDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccountingDocument>> GetByTypeAsync(Guid tenantId, DocumentType type, CancellationToken ct = default);
    Task<IReadOnlyList<AccountingDocument>> GetByCounterpartyAsync(Guid tenantId, Guid counterpartyId, CancellationToken ct = default);
    Task AddAsync(AccountingDocument document, CancellationToken ct = default);
    Task UpdateAsync(AccountingDocument document, CancellationToken ct = default);
}
