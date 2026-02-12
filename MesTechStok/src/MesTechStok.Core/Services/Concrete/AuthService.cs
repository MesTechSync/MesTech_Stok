using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;
using System.Security.Cryptography;
using System.Text;

namespace MesTechStok.Core.Services.Concrete;

/// <summary>
/// Authentication service implementation - FAZ 1 GÖREV 1.1
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private User? _currentUser;

    public AuthService(AppDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        // DEMO MODE: Configurable authentication - accepts blank or Admin123!
        var normalizedUser = username?.Trim().ToLower();
        var normalizedPass = password?.Trim() ?? string.Empty;

        // Accept: username 'admin' with either empty password or 'Admin123!'
        if (normalizedUser == "admin" && (string.IsNullOrEmpty(normalizedPass) || normalizedPass == "Admin123!"))
        {
            var demoUser = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@mestech.com",
                FirstName = "Demo",
                LastName = "Administrator",
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _currentUser = demoUser; // Set current user
            _logger.LogInformation("Demo login successful for user: {Username}", username);

            return new AuthResult
            {
                IsSuccess = true,
                User = demoUser,
                Message = "Demo login successful"
            };
        }

        _logger.LogWarning("Invalid login attempt for user: {Username}", username);
        return new AuthResult { IsSuccess = false, Message = "Invalid credentials - use: admin / Admin123!" };
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
        // Simple hash for demo - in production use BCrypt or similar
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT"));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hashedPassword;
    }
}