using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Auth;

/// <summary>
/// OWASP ASVS V4 — Role→RolePermission→Permission zinciri ile yetkilendirme.
/// User→UserRole→Role→RolePermission→Permission join'i yaparak kontrol eder.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(AppDbContext context, ILogger<PermissionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        var hasPermission = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => _context.RolePermissions
                .Where(rp => rp.RoleId == ur.RoleId))
            .Join(_context.Permissions,
                rp => rp.PermissionId,
                p => p.Id,
                (rp, p) => p)
            .AnyAsync(p => p.Name == permissionName && p.IsActive, ct)
            .ConfigureAwait(false);

        if (!hasPermission)
            _logger.LogWarning("Permission denied: User={UserId}, Permission={Permission}", userId, permissionName);

        return hasPermission;
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => _context.RolePermissions
                .Where(rp => rp.RoleId == ur.RoleId))
            .Join(_context.Permissions,
                rp => rp.PermissionId,
                p => p.Id,
                (rp, p) => p)
            .Where(p => p.IsActive)
            .Select(p => p.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r)
            .AnyAsync(r => r.Name == roleName && r.IsActive, ct)
            .ConfigureAwait(false);
    }
}
