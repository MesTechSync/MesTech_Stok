using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Auth.Commands.Authenticate;

/// <summary>
/// Kullanici kimlik dogrulama handler'i — IAuthService uzerinden BCrypt dogrulama yapar.
/// Avalonia LoginVM bu handler'i kullanarak giris islemini gerceklestirir.
/// </summary>
public sealed class AuthenticateHandler : IRequestHandler<AuthenticateCommand, AuthenticateResult>
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService? _jwtService;
    private readonly ILogger<AuthenticateHandler> _logger;

    public AuthenticateHandler(
        IAuthService authService,
        IUserRepository userRepo,
        ILogger<AuthenticateHandler> logger,
        IJwtTokenService? jwtService = null)
    {
        _authService = authService;
        _userRepo = userRepo;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<AuthenticateResult> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            var authResult = await _authService.ValidateAsync(request.Username, request.Password, cancellationToken).ConfigureAwait(false);

            if (!authResult.IsSuccess)
            {
                _logger.LogWarning("Basarisiz giris denemesi: Username={Username}", request.Username);
                return AuthenticateResult.Failure(authResult.ErrorMessage ?? "Gecersiz kullanici adi veya sifre.");
            }

            var user = await _userRepo.GetByUsernameAsync(request.Username, cancellationToken).ConfigureAwait(false);
            var role = user?.GetType().GetProperty("Role")?.GetValue(user)?.ToString();

            var tenantId = user?.TenantId ?? Guid.Empty;
            var roles = role != null ? new[] { role } : Array.Empty<string>();
            var token = _jwtService?.GenerateToken(authResult.UserId!.Value, tenantId, request.Username, roles)
                        ?? Guid.NewGuid().ToString("N"); // fallback if IJwtTokenService not registered (Desktop DI)
            var refreshToken = _jwtService?.GenerateRefreshToken()
                               ?? Guid.NewGuid().ToString("N");

            _logger.LogInformation(
                "Basarili giris: Username={Username}, UserId={UserId}",
                request.Username, authResult.UserId);

            return AuthenticateResult.Success(
                authResult.UserId!.Value,
                authResult.DisplayName ?? request.Username,
                role,
                token,
                refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kimlik dogrulama hatasi: Username={Username}", request.Username);
            return AuthenticateResult.Failure("Beklenmeyen bir hata olustu.");
        }
#pragma warning restore CA1031
    }
}
