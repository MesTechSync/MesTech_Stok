using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MesTech.Infrastructure.Auth;

/// <summary>
/// JWT token service for Blazor SaaS authentication (Dalga 9).
/// Uses IOptions&lt;JwtTokenOptions&gt; bound from appsettings "Jwt" section.
/// Secret MUST be stored in user-secrets or environment variables — never hardcode.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtTokenOptions _options;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtTokenOptions> options, ILogger<JwtTokenService> logger)
    {
        _logger = logger;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Secret))
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set it via user-secrets or environment variables.");

        if (_options.Secret.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 characters for HMAC-SHA256.");
    }

    public string GenerateToken(Guid userId, Guid tenantId, string userName, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation(
            "JWT generated for User={UserId} Tenant={TenantId} Roles={Roles} ExpiresIn={Minutes}min",
            userId, tenantId, string.Join(",", roles), _options.ExpiryMinutes);

        return tokenString;
    }

    public (bool Valid, Guid UserId, Guid TenantId) ValidateToken(string token)
        => ValidateTokenInternal(token, validateLifetime: true);

    public (bool Valid, Guid UserId, Guid TenantId) ValidateTokenIgnoreExpiry(string token)
        => ValidateTokenInternal(token, validateLifetime: false);

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private (bool Valid, Guid UserId, Guid TenantId) ValidateTokenInternal(string token, bool validateLifetime)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
            var handler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = handler.ValidateToken(token, parameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwt
                || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("JWT validation failed: invalid algorithm");
                return (false, Guid.Empty, Guid.Empty);
            }

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            var tenantIdClaim = principal.FindFirst("tenant_id");

            if (userIdClaim is null || tenantIdClaim is null
                || !Guid.TryParse(userIdClaim.Value, out var userId)
                || !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                _logger.LogWarning("JWT validation failed: missing sub or tenant_id claim");
                return (false, Guid.Empty, Guid.Empty);
            }

            return (true, userId, tenantId);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT validation failed: token expired");
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT validation failed: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
    }
}
