using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

/// <summary>
/// Authentication servisi interface - FAZ 1 GÖREV 1.1
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<bool> LogoutAsync(int userId);
    Task<User?> GetCurrentUserAsync();
    Task<bool> IsUserInRoleAsync(int userId, string roleName);
    Task<List<string>> GetUserRolesAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, string permissionName, string module);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

/// <summary>
/// Authentication sonuç modeli
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}