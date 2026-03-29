using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class FulfillmentShipmentRepository : IFulfillmentShipmentRepository
{
    private readonly AppDbContext _db;

    public FulfillmentShipmentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FulfillmentShipment>> GetByTenantAsync(
        Guid tenantId, string? center = null, string? status = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _db.Set<FulfillmentShipment>()
            .Where(f => f.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(center))
            query = query.Where(f => f.Center == center);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<int> CountByTenantAsync(
        Guid tenantId, string? center = null, string? status = null,
        CancellationToken ct = default)
    {
        var query = _db.Set<FulfillmentShipment>()
            .Where(f => f.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(center))
            query = query.Where(f => f.Center == center);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        return await query.CountAsync(ct);
    }

    public async Task AddAsync(FulfillmentShipment shipment, CancellationToken ct = default)
    {
        await _db.Set<FulfillmentShipment>().AddAsync(shipment, ct);
    }
}
