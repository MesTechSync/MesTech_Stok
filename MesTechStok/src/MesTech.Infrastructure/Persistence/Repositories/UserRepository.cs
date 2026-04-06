using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

// DEV6: User entity ITenantEntity — global query filter aktif.
// Login sırasında tenant_id henüz bilinmez → IgnoreQueryFilters() zorunlu.
public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct).ConfigureAwait(false);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await db.Users
            .IgnoreQueryFilters() // Login akışında JWT yok → TenantId bilinmez → filter bypass
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking().OrderBy(u => u.Username).Take(1000) // G485: pagination guard
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct).ConfigureAwait(false);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.Users.CountAsync(u => u.TenantId == tenantId, ct).ConfigureAwait(false);
}
