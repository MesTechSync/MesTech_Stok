using MesTech.Domain.Accounting.Entities;

namespace MesTech.Domain.Interfaces;

public interface IAccountingPeriodRepository
{
    Task<IReadOnlyList<AccountingPeriod>> GetByTenantAsync(
        Guid tenantId, int? year = null, CancellationToken ct = default);
    Task<AccountingPeriod?> GetByYearMonthAsync(
        Guid tenantId, int year, int month, CancellationToken ct = default);
    Task AddAsync(AccountingPeriod period, CancellationToken ct = default);
}
