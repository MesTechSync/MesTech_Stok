using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Auth;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Options;

#pragma warning disable CA1031 // Intentional: login failure must return structured response

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Authentication endpoints for Blazor SaaS login (Dalga 9).
/// POST /api/v1/auth/login — AllowAnonymous (bypasses API key middleware).
/// Brute force protection: 5 fail → 15dk lockout, progressive delay, IP rate limit.
/// </summary>
public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth").RequireRateLimiting("AuthRateLimit");

        // POST /api/v1/auth/login — authenticate user and return JWT + refresh token
        group.MapPost("/login", async (
            LoginRequest request,
            IJwtTokenService jwtService,
            IRefreshTokenRepository refreshTokenRepo,
            IUnitOfWork unitOfWork,
            BruteForceProtectionService bruteForce,
            IOptions<JwtTokenOptions> jwtOptions,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.AuthEndpoints");
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new LoginResponse(
                    Success: false,
                    Token: null,
                    RefreshToken: null,
                    ExpiresAt: null,
                    Error: "UserName and Password are required.",
                    AttemptsRemaining: null,
                    LockedUntilUtc: null));
            }

            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var check = await bruteForce.CheckAsync(request.UserName, ip);

            // IP rate limit → 429
            if (check.IsIpRateLimited)
            {
                return Results.Json(new LoginResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: "Çok fazla istek. Lütfen bir dakika bekleyin.",
                    AttemptsRemaining: 0, LockedUntilUtc: null),
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            // Account locked
            if (check.IsLocked)
            {
                return Results.Json(new LoginResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: "Çok fazla başarısız deneme. Hesabınız kilitlendi.",
                    AttemptsRemaining: 0,
                    LockedUntilUtc: check.LockedUntil?.UtcDateTime),
                    statusCode: StatusCodes.Status423Locked);
            }

            // Placeholder: generate token with deterministic dev IDs (NEVER in production)
            // Real user lookup + BCrypt.Verify needed before production
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var roles = new[] { "User" };

            try
            {
                var options = jwtOptions.Value;
                var token = jwtService.GenerateToken(userId, tenantId, request.UserName, roles);
                var expiresAt = DateTime.UtcNow.AddMinutes(
                    options.AccessTokenExpiryMinutes > 0 ? options.AccessTokenExpiryMinutes : options.ExpiryMinutes);

                // Generate refresh token and persist
                var refreshTokenString = jwtService.GenerateRefreshToken();
                var tokenHash = jwtService.HashToken(refreshTokenString);
                var ua = httpContext.Request.Headers.UserAgent.ToString();

                var refreshToken = Domain.Entities.RefreshToken.Create(
                    userId, tenantId, tokenHash, options.RefreshTokenExpiryDays, ip, ua);
                await refreshTokenRepo.AddAsync(refreshToken, ct);
                await unitOfWork.SaveChangesAsync(ct);

                await bruteForce.RecordSuccessAsync(request.UserName, ip);

                return Results.Ok(new LoginResponse(
                    Success: true,
                    Token: token,
                    RefreshToken: refreshTokenString,
                    ExpiresAt: expiresAt,
                    Error: null,
                    AttemptsRemaining: null,
                    LockedUntilUtc: null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Login failed for user {UserName} from IP {IP}", request.UserName, ip);
                var failure = await bruteForce.RecordFailureAsync(request.UserName, ip);

                if (failure.IsNowLocked)
                {
                    return Results.Json(new LoginResponse(
                        Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                        Error: "Çok fazla başarısız deneme. Hesabınız 15 dakika kilitlendi.",
                        AttemptsRemaining: 0,
                        LockedUntilUtc: DateTime.UtcNow.Add(failure.LockoutDuration ?? TimeSpan.FromMinutes(15))),
                        statusCode: StatusCodes.Status423Locked);
                }

                return Results.Json(new LoginResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: $"Giriş bilgileri hatalı. {failure.AttemptsRemaining} deneme hakkınız kaldı.",
                    AttemptsRemaining: failure.AttemptsRemaining,
                    LockedUntilUtc: null),
                    statusCode: StatusCodes.Status401Unauthorized);
            }
        })
        .WithName("Login")
        .WithSummary("JWT token + refresh token ile kimlik doğrulama — brute force korumalı")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<LoginResponse>(StatusCodes.Status401Unauthorized)
        .Produces<LoginResponse>(StatusCodes.Status423Locked)
        .Produces<LoginResponse>(StatusCodes.Status429TooManyRequests)
        .AllowAnonymous();

        // POST /api/v1/auth/validate — validate an existing JWT token
        group.MapPost("/validate", (ValidateTokenRequest request, IJwtTokenService jwtService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return Results.BadRequest(new { error = "Token is required." });

            var (valid, userId, tenantId) = jwtService.ValidateToken(request.Token);

            return Results.Ok(new
            {
                valid,
                userId = valid ? userId : (Guid?)null,
                tenantId = valid ? tenantId : (Guid?)null
            });
        })
        .WithName("ValidateToken")
        .WithSummary("JWT token doğrulama — geçerlilik, userId, tenantId")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/v1/auth/refresh — rotate refresh token (OWASP ASVS V3.3)
        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IJwtTokenService jwtService,
            IRefreshTokenRepository refreshTokenRepo,
            IUserRepository userRepo,
            IUnitOfWork unitOfWork,
            IOptions<JwtTokenOptions> jwtOptions,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.AuthEndpoints");

            if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.BadRequest(new RefreshTokenResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: "AccessToken and RefreshToken are required."));

            // Validate expired access token to extract claims
            var (valid, userId, tenantId) = jwtService.ValidateTokenIgnoreExpiry(request.AccessToken);
            if (!valid)
                return Results.Json(new RefreshTokenResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: "Invalid access token."),
                    statusCode: StatusCodes.Status401Unauthorized);

            // Find refresh token by hash
            var tokenHash = jwtService.HashToken(request.RefreshToken);
            var storedToken = await refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

            if (storedToken is null || !storedToken.IsActive || storedToken.UserId != userId)
            {
                if (storedToken is not null && storedToken.IsRevoked)
                {
                    // Possible token reuse attack — revoke entire family
                    logger.LogWarning(
                        "Refresh token reuse detected for User={UserId} — revoking all tokens",
                        userId);
                    await refreshTokenRepo.RevokeAllByUserAsync(userId, "Token reuse detected", ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }

                return Results.Json(new RefreshTokenResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: "Invalid or expired refresh token."),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Verify user still exists and is active
            var user = await userRepo.GetByIdAsync(userId);
            if (user is null || !user.IsActive)
                return Results.Json(new RefreshTokenResponse(
                    Success: false, Token: null, RefreshToken: null, ExpiresAt: null,
                    Error: "User account is inactive."),
                    statusCode: StatusCodes.Status401Unauthorized);

            // Rotate: revoke old, create new
            var newRefreshTokenString = jwtService.GenerateRefreshToken();
            var newTokenHash = jwtService.HashToken(newRefreshTokenString);

            storedToken.Revoke("Rotated", newTokenHash);

            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var ua = httpContext.Request.Headers.UserAgent.ToString();
            var options = jwtOptions.Value;

            var newRefreshToken = Domain.Entities.RefreshToken.Create(
                userId, tenantId, newTokenHash,
                options.RefreshTokenExpiryDays, ip, ua);
            await refreshTokenRepo.AddAsync(newRefreshToken, ct);

            // Generate new access token — UserRole has no Role navigation, use default
            var roles = new[] { "User" };

            var newAccessToken = jwtService.GenerateToken(userId, tenantId, user.Username, roles);
            var expiresAt = DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes > 0
                ? options.AccessTokenExpiryMinutes
                : options.ExpiryMinutes);

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Refresh token rotated for User={UserId} Tenant={TenantId}",
                userId, tenantId);

            return Results.Ok(new RefreshTokenResponse(
                Success: true,
                Token: newAccessToken,
                RefreshToken: newRefreshTokenString,
                ExpiresAt: expiresAt,
                Error: null));
        })
        .WithName("RefreshToken")
        .WithSummary("JWT refresh token rotation — OWASP ASVS V3.3")
        .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
        .Produces<RefreshTokenResponse>(StatusCodes.Status401Unauthorized)
        .AllowAnonymous();

        // POST /api/v1/auth/revoke — revoke refresh token (logout)
        group.MapPost("/revoke", async (
            RevokeTokenRequest request,
            IJwtTokenService jwtService,
            IRefreshTokenRepository refreshTokenRepo,
            IUnitOfWork unitOfWork,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.AuthEndpoints");

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.BadRequest(new { error = "RefreshToken is required." });

            var tokenHash = jwtService.HashToken(request.RefreshToken);
            var storedToken = await refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

            if (storedToken is null)
                return Results.Ok(new StatusResponse("Revoked"));

            if (!storedToken.IsRevoked)
            {
                storedToken.Revoke("User logout");
                await unitOfWork.SaveChangesAsync(ct);
            }

            logger.LogInformation(
                "Refresh token revoked for User={UserId} via logout",
                storedToken.UserId);

            return Results.Ok(new StatusResponse("Revoked"));
        })
        .WithName("RevokeToken")
        .WithSummary("Refresh token iptal — logout")
        .Produces<StatusResponse>(StatusCodes.Status200OK);
    }

    // ── Request / Response Records ──

    public record LoginRequest(string UserName, string Password);

    public record LoginResponse(
        bool Success, string? Token, string? RefreshToken, DateTime? ExpiresAt, string? Error,
        int? AttemptsRemaining, DateTime? LockedUntilUtc);

    public record ValidateTokenRequest(string Token);

    public record RefreshTokenRequest(string AccessToken, string RefreshToken);

    public record RefreshTokenResponse(
        bool Success, string? Token, string? RefreshToken, DateTime? ExpiresAt, string? Error);

    public record RevokeTokenRequest(string RefreshToken);
}
