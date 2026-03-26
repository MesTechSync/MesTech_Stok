using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Auth;

/// <summary>
/// BCrypt-based user authentication service.
/// Validates username/password against User entity PasswordHash.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepo, ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<AuthResult> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Kullanıcı adı ve şifre boş bırakılamaz.");

        var user = await _userRepo.GetByUsernameAsync(username);

        if (user is null)
        {
            _logger.LogWarning("Login attempt failed — user not found: {Username}", username);
            return AuthResult.Fail("Geçersiz kullanıcı adı veya şifre.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for disabled user: {Username}", username);
            return AuthResult.Fail("Hesap devre dışı.");
        }

        bool passwordValid;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BCrypt verification failed for user {Username}", username);
            return AuthResult.Fail("Kimlik doğrulama hatası.");
        }

        if (!passwordValid)
        {
            _logger.LogWarning("Login attempt failed — invalid password for user: {Username}", username);
            return AuthResult.Fail("Geçersiz kullanıcı adı veya şifre.");
        }

        _logger.LogInformation("User {Username} authenticated successfully. UserId={UserId}", username, user.Id);

        return AuthResult.Success(
            user.Id,
            user.TenantId,
            user.FullName ?? user.Username);
    }
}
