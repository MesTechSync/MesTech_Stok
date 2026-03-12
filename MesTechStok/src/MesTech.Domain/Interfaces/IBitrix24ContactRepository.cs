using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IBitrix24ContactRepository
{
    Task<Bitrix24Contact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Bitrix24Contact?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<Bitrix24Contact?> GetByExternalContactIdAsync(string externalContactId, CancellationToken ct = default);
    Task<IReadOnlyList<Bitrix24Contact>> GetUnsyncedAsync(CancellationToken ct = default);
    Task AddAsync(Bitrix24Contact contact, CancellationToken ct = default);
    Task UpdateAsync(Bitrix24Contact contact, CancellationToken ct = default);
}
