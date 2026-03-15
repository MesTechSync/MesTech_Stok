using MesTech.Domain.Dropshipping.Entities;

namespace MesTech.Application.Interfaces.Dropshipping;

/// <summary>
/// Dropshipping sipariş veri erişim arayüzü.
/// </summary>
public interface IDropshipOrderRepository
{
    Task<DropshipOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropshipOrder>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<DropshipOrder?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task AddAsync(DropshipOrder order, CancellationToken ct = default);
    Task UpdateAsync(DropshipOrder order, CancellationToken ct = default);
}
