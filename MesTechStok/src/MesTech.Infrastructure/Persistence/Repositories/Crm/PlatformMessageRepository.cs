using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

public class PlatformMessageRepository : IPlatformMessageRepository
{
    private readonly AppDbContext _context;

    public PlatformMessageRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<PlatformMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PlatformMessages
            .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<PlatformMessage> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, PlatformType? platform, MessageStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.PlatformMessages
            .Where(m => m.TenantId == tenantId && !m.IsDeleted);

        if (platform.HasValue)
            query = query.Where(m => m.Platform == platform.Value);
        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(m => m.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<int> CountByStatusAsync(Guid tenantId, MessageStatus status, CancellationToken ct = default)
        => await _context.PlatformMessages
            .CountAsync(m => m.TenantId == tenantId && m.Status == status && !m.IsDeleted, ct)
            .ConfigureAwait(false);

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.PlatformMessages
            .CountAsync(m => m.TenantId == tenantId && !m.IsDeleted, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(PlatformMessage message, CancellationToken ct = default)
        => await _context.PlatformMessages.AddAsync(message, ct).ConfigureAwait(false);
}
