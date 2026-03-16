using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// StoreCredential CRUD repository — credential key/value ciftleri icin.
/// </summary>
public interface IStoreCredentialRepository
{
    Task<StoreCredential?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StoreCredential>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task AddAsync(StoreCredential credential, CancellationToken ct = default);
    Task UpdateAsync(StoreCredential credential, CancellationToken ct = default);
    Task DeleteAsync(StoreCredential credential, CancellationToken ct = default);
}
