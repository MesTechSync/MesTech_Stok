using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ICariHesapRepository
{
    Task<CariHesap?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CariHesap>> GetByTypeAsync(CariHesapType type, Guid? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<CariHesap>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task<CariHesap?> GetByNameAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task AddAsync(CariHesap cariHesap, CancellationToken ct = default);
    Task UpdateAsync(CariHesap cariHesap, CancellationToken ct = default);
}
