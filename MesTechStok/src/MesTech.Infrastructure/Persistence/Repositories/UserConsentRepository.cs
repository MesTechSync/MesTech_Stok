using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class UserConsentRepository : IUserConsentRepository
{
    private readonly AppDbContext _context;

    public UserConsentRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(UserConsent consent, CancellationToken ct = default)
        => await _context.UserConsents.AddAsync(consent, ct).ConfigureAwait(false);

    public async Task<UserConsent?> GetActiveConsentAsync(
        Guid tenantId, Guid userId, ConsentType consentType, CancellationToken ct = default)
    {
        return await _context.UserConsents
            .Where(c => c.TenantId == tenantId && c.UserId == userId
                && c.ConsentType == consentType && c.IsAccepted)
            .OrderByDescending(c => c.AcceptedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserConsent>> GetByUserAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        return await _context.UserConsents
            .Where(c => c.TenantId == tenantId && c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
