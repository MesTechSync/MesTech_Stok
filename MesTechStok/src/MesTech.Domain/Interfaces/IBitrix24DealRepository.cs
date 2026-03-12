using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IBitrix24DealRepository
{
    Task<Bitrix24Deal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Bitrix24Deal?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Bitrix24Deal?> GetByExternalDealIdAsync(string externalDealId, CancellationToken ct = default);
    Task AddAsync(Bitrix24Deal deal, CancellationToken ct = default);
    Task UpdateAsync(Bitrix24Deal deal, CancellationToken ct = default);
}
