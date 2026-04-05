using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ICariHareketRepository
{
    Task<IReadOnlyList<CariHareket>> GetByCariHesapIdAsync(Guid cariHesapId, CancellationToken ct = default);
    Task<IReadOnlyList<CariHareket>> GetByDateRangeAsync(Guid cariHesapId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<CariHareket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(CariHareket hareket, CancellationToken ct = default);
}
