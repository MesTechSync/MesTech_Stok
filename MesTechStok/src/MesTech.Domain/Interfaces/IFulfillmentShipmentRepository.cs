using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IFulfillmentShipmentRepository
{
    Task<IReadOnlyList<FulfillmentShipment>> GetByTenantAsync(
        Guid tenantId, string? center = null, string? status = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default);

    Task<int> CountByTenantAsync(
        Guid tenantId, string? center = null, string? status = null,
        CancellationToken ct = default);

    Task AddAsync(FulfillmentShipment shipment, CancellationToken ct = default);
}
