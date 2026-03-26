using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IUserConsentRepository
{
    Task AddAsync(UserConsent consent, CancellationToken ct = default);
    Task<UserConsent?> GetActiveConsentAsync(Guid tenantId, Guid userId, ConsentType consentType, CancellationToken ct = default);
    Task<IReadOnlyList<UserConsent>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
}
