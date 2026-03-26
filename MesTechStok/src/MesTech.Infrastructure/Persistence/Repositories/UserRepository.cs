using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

// DEV6-TUR11: User entity ITenantEntity değil — global filter yok.
// FindAsync → FirstOrDefaultAsync (query filter uyumlu).
// G028: DEV 1'e atanacak — User entity'ye ITenantEntity eklenmeli.
public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users.OrderBy(u => u.Username).ToListAsync(ct);

    public async Task AddAsync(User user) =>
        await db.Users.AddAsync(user);

    public Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.Users.CountAsync(u => u.TenantId == tenantId, ct);
}
