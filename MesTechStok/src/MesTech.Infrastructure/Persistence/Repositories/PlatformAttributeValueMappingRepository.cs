using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class PlatformAttributeValueMappingRepository : IPlatformAttributeValueMappingRepository
{
    private readonly AppDbContext _context;
    public PlatformAttributeValueMappingRepository(AppDbContext context) => _context = context;

    public async Task<PlatformAttributeValueMapping?> GetByInternalValueAsync(
        Guid tenantId, string attributeName, string value, PlatformType platform, CancellationToken ct = default)
        => await _context.PlatformAttributeValueMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId
                                    && x.InternalAttributeName == attributeName
                                    && x.InternalValue == value
                                    && x.PlatformType == platform, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PlatformAttributeValueMapping>> GetByPlatformAsync(
        Guid tenantId, PlatformType platform, CancellationToken ct = default)
        => await _context.PlatformAttributeValueMappings
            .Where(x => x.TenantId == tenantId && x.PlatformType == platform)
            .OrderBy(x => x.InternalAttributeName).ThenBy(x => x.InternalValue)
            .Take(1000)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(PlatformAttributeValueMapping mapping, CancellationToken ct = default)
        => await _context.PlatformAttributeValueMappings.AddAsync(mapping, ct).ConfigureAwait(false);

    public Task UpdateAsync(PlatformAttributeValueMapping mapping, CancellationToken ct = default)
    {
        _context.PlatformAttributeValueMappings.Update(mapping);
        return Task.CompletedTask;
    }
}
