using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ITaxCalendarItemRepository
{
    Task<TaxCalendarItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaxCalendarItem>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(TaxCalendarItem item, CancellationToken ct = default);
    Task UpdateAsync(TaxCalendarItem item, CancellationToken ct = default);
}
