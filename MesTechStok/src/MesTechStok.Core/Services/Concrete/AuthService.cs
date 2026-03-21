using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;
using BCrypt.Net;

namespace MesTechStok.Core.Services.Concrete;

/// <summary>
/// Authentication service implementation — BCrypt + brute-force korumalı
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private User? _currentUser;

    // Brute-force koruma
    private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _loginAttempts = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);

    // Admin identity from environment — no hardcoded values
    private static readonly string _adminUsername =
        Environment.GetEnvironmentVariable("MESTECH_ADMIN_USER")
        ?? throw new InvalidOperationException("MESTECH_ADMIN_USER env var required. Set it before starting the application.");
    private static readonly string _adminEmail =
        Environment.GetEnvironmentVariable("MESTECH_ADMIN_EMAIL")
        ?? throw new InvalidOperationException("MESTECH_ADMIN_EMAIL env var required. Set it before starting the application.");

    // DB erişilemezse fallback hash — generated at startup from env var only
    private static readonly string _fallbackAdminHash =
        Environment.GetEnvironmentVariable("MESTECH_ADMIN_HASH")
        ?? BCrypt.Net.BCrypt.HashPassword(
            Environment.GetEnvironmentVariable("MESTECH_ADMIN_PASSWORD")
            ?? throw new InvalidOperationException("MESTECH_ADMIN_PASSWORD or MESTECH_ADMIN_HASH env var required."),
            workFactor: 12);

    public AuthService(AppDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var normalizedUser = (username?.Trim().ToLower()) ?? string.Empty;
        var normalizedPass = password?.Trim() ?? string.Empty;

        // Boş kimlik bilgileri reddet
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedPass))
        {
            return new AuthResult { IsSuccess = false, Message = "Kullanici adi ve sifre gereklidir" };
        }

        // Brute-force kilitleme kontrolü
        if (IsLockedOut(normalizedUser))
        {
            _logger.LogWarning("Account locked out: {Username}", normalizedUser);
            return new AuthResult { IsSuccess = false, Message = "Hesabiniz kilitlendi. 1 dakika bekleyin." };
        }

        try
        {
            // DB'den kullanıcı ara
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUser && u.IsActive);

            // DB'de hiç kullanıcı yoksa default admin seed et
            if (user == null && !await _context.Users.AnyAsync())
            {
                await SeedDefaultAdminAsync();
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUser && u.IsActive);
            }

            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                if (BCrypt.Net.BCrypt.Verify(normalizedPass, user.PasswordHash))
                {
                    // Başarılı giriş — sayacı sıfırla
                    _loginAttempts.TryRemove(normalizedUser, out _);
                    user.LastLoginDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _currentUser = user;
                    _logger.LogInformation("Login successful for user: {Username}", username);
                    return new AuthResult { IsSuccess = true, User = user, Message = "Giris basarili" };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB auth error for user: {Username} — falling back to static hash", username);

            // DB erişilemezse: sadece env var'dan gelen admin kullanıcısı için BCrypt fallback
            if (normalizedUser == _adminUsername.ToLowerInvariant() && BCrypt.Net.BCrypt.Verify(normalizedPass, _fallbackAdminHash))
            {
                _loginAttempts.TryRemove(normalizedUser, out _);
                _currentUser = new User
                {
                    Id = 1, Username = _adminUsername, Email = _adminEmail,
                    FirstName = "Admin", LastName = "MesTech", FullName = "Admin MesTech",
                    IsActive = true, CreatedDate = DateTime.UtcNow
                };
                _logger.LogWarning("Fallback login used for admin (DB unavailable)");
                return new AuthResult { IsSuccess = true, User = _currentUser, Message = "Giris basarili (fallback)" };
            }
        }

        // Başarısız giriş — sayacı artır
        RecordFailedAttempt(normalizedUser);
        _logger.LogWarning("Invalid login attempt for user: {Username}", username);
        return new AuthResult { IsSuccess = false, Message = "Gecersiz kullanici adi veya sifre" };
    }

    private async Task SeedDefaultAdminAsync()
    {
        try
        {
            var adminUser = new User
            {
                Username = _adminUsername,
                Email = _adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    Environment.GetEnvironmentVariable("MESTECH_ADMIN_PASSWORD")
                    ?? throw new InvalidOperationException("MESTECH_ADMIN_PASSWORD env var required for seed."),
                    workFactor: 12),
                FirstName = "Admin",
                LastName = "MesTech",
                FullName = "Admin MesTech",
                IsActive = true,
                IsEmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Default admin user seeded with BCrypt hash");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed default admin user");
        }
    }

    private bool IsLockedOut(string username)
    {
        if (!_loginAttempts.TryGetValue(username, out var info)) return false;
        if (info.Count >= MaxAttempts && DateTime.UtcNow - info.LastAttempt < LockoutDuration)
            return true;
        if (DateTime.UtcNow - info.LastAttempt >= LockoutDuration)
            _loginAttempts.TryRemove(username, out _);
        return false;
    }

    private void RecordFailedAttempt(string username)
    {
        _loginAttempts.AddOrUpdate(
            username,
            _ => (1, DateTime.UtcNow),
            (_, existing) => (existing.Count + 1, DateTime.UtcNow));
    }

    public async Task<bool> LogoutAsync(int userId)
    {
        try
        {
            _currentUser = null;
            _logger.LogInformation("User {UserId} logged out", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        return _currentUser;
    }

    public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
    {
        try
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName && ur.Role.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role {RoleName} for user {UserId}", roleName, userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        try
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && ur.Role.IsActive)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionName, string module)
    {
        try
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Where(ur => ur.UserId == userId && ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Name == permissionName &&
                              rp.Permission.Module == module &&
                              rp.Permission.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionName} in module {Module} for user {UserId}",
                           permissionName, module, userId);
            return false;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}