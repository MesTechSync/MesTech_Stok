using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class SocialFeedConfigurationRepository : ISocialFeedConfigurationRepository
{
    private readonly AppDbContext _context;

    public SocialFeedConfigurationRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<SocialFeedConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SocialFeedConfigurations
            .FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<SocialFeedConfiguration>> GetActiveAsync(CancellationToken ct = default)
        => await _context.SocialFeedConfigurations.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Platform)
            .Take(1000) // G485: pagination guard
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<SocialFeedConfiguration?> GetByTenantAndPlatformAsync(
        Guid tenantId, SocialFeedPlatform platform, CancellationToken ct = default)
        => await _context.SocialFeedConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Platform == platform, ct)
            .ConfigureAwait(false);

    public Task UpdateAsync(SocialFeedConfiguration config, CancellationToken ct = default)
    {
        _context.SocialFeedConfigurations.Update(config);
        return Task.CompletedTask;
    }
}
