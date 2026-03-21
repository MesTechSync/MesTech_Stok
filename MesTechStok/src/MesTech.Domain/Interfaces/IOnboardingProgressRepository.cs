using MesTech.Domain.Entities.Onboarding;

namespace MesTech.Domain.Interfaces;

public interface IOnboardingProgressRepository
{
    Task<OnboardingProgress?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(OnboardingProgress progress, CancellationToken ct = default);
    Task UpdateAsync(OnboardingProgress progress, CancellationToken ct = default);
}
