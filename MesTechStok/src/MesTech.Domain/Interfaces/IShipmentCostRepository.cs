using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IShipmentCostRepository
{
    Task<IReadOnlyList<ShipmentCost>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<ShipmentCost>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task AddAsync(ShipmentCost cost, CancellationToken ct = default);
}
