using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ICariHareketRepository
{
    Task<IReadOnlyList<CariHareket>> GetByCariHesapIdAsync(Guid cariHesapId);
    Task<IReadOnlyList<CariHareket>> GetByDateRangeAsync(Guid cariHesapId, DateTime from, DateTime to);
    Task AddAsync(CariHareket hareket);
}
