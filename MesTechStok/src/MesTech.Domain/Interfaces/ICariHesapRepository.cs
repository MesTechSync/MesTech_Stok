using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ICariHesapRepository
{
    Task<CariHesap?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<CariHesap>> GetByTypeAsync(CariHesapType type, Guid? tenantId = null);
    Task<IReadOnlyList<CariHesap>> GetAllAsync(Guid? tenantId = null);
    Task AddAsync(CariHesap cariHesap);
    Task UpdateAsync(CariHesap cariHesap);
}
