using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IAccountingSupplierAccountRepository
{
    Task<AccountingSupplierAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AccountingSupplierAccount?> GetBySupplierIdAsync(Guid tenantId, Guid supplierId, CancellationToken ct = default);
    Task<IReadOnlyList<AccountingSupplierAccount>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(AccountingSupplierAccount account, CancellationToken ct = default);
    Task UpdateAsync(AccountingSupplierAccount account, CancellationToken ct = default);
}
