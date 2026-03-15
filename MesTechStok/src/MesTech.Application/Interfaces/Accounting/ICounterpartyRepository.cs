using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface ICounterpartyRepository
{
    Task<Counterparty?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Counterparty>> GetAllAsync(Guid tenantId, CounterpartyType? type = null, bool? isActive = null, CancellationToken ct = default);
    Task<Counterparty?> GetByVknAsync(Guid tenantId, string vkn, CancellationToken ct = default);
    Task AddAsync(Counterparty counterparty, CancellationToken ct = default);
    Task UpdateAsync(Counterparty counterparty, CancellationToken ct = default);
}
