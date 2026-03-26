using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await _context.Set<RefreshToken>().AddAsync(token, ct).ConfigureAwait(false);

    public async Task RevokeAllByUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var activeTokens = await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in activeTokens)
            token.Revoke(reason);
    }
}
