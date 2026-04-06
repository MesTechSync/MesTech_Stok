using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// Role CRUD repository — HH-DEV6-005.
/// </summary>
public sealed class RoleRepository(AppDbContext db) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking().OrderBy(r => r.Name).Take(500)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await db.Roles.AddAsync(role, ct).ConfigureAwait(false);

    public Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        db.Roles.Update(role);
        return Task.CompletedTask;
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct).ConfigureAwait(false);
}
