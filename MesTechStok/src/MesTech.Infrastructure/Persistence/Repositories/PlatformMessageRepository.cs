using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core backed platform message repository.
/// </summary>
public sealed class PlatformMessageRepository : IPlatformMessageRepository
{
    private readonly AppDbContext _dbContext;

    public PlatformMessageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.PlatformMessages
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<(IReadOnlyList<PlatformMessage> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, PlatformType? platform, MessageStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.PlatformMessages
            .Where(m => m.TenantId == tenantId);

        if (platform.HasValue)
            query = query.Where(m => m.Platform == platform.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountByStatusAsync(Guid tenantId, MessageStatus status, CancellationToken ct = default)
    {
        return await _dbContext.PlatformMessages
            .CountAsync(m => m.TenantId == tenantId && m.Status == status, ct);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _dbContext.PlatformMessages
            .CountAsync(m => m.TenantId == tenantId, ct);
    }

    public async Task AddAsync(PlatformMessage message, CancellationToken ct = default)
    {
        await _dbContext.PlatformMessages.AddAsync(message, ct);
    }
}
