using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class OnboardingProgressRepository : IOnboardingProgressRepository
{
    private readonly AppDbContext _context;

    public OnboardingProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingProgress?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.OnboardingProgress
            .AsNoTracking().FirstOrDefaultAsync(o => o.TenantId == tenantId, ct).ConfigureAwait(false);

    public async Task AddAsync(OnboardingProgress progress, CancellationToken ct = default)
        => await _context.OnboardingProgress.AddAsync(progress, ct).ConfigureAwait(false);

    public Task UpdateAsync(OnboardingProgress progress, CancellationToken ct = default)
    {
        _context.OnboardingProgress.Update(progress);
        return Task.CompletedTask;
    }
}
