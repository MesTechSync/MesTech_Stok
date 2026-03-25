using MesTech.Application.Interfaces;
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
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // POST /api/v1/auth/login — authenticate user and return JWT
        group.MapPost("/login", async (
            LoginRequest request,
            IJwtTokenService jwtService,
            BruteForceProtectionService bruteForce,
            IOptions<JwtTokenOptions> jwtOptions,
            ILoggerFactory loggerFactory,
            HttpContext httpContext) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.AuthEndpoints");
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new LoginResponse(
                    Success: false,
                    Token: null,
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
                    Success: false, Token: null, ExpiresAt: null,
                    Error: "Çok fazla istek. Lütfen bir dakika bekleyin.",
                    AttemptsRemaining: 0, LockedUntilUtc: null),
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            // Account locked
            if (check.IsLocked)
            {
                return Results.Json(new LoginResponse(
                    Success: false, Token: null, ExpiresAt: null,
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
                var token = jwtService.GenerateToken(userId, tenantId, request.UserName, roles);
                var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpiryMinutes);

                await bruteForce.RecordSuccessAsync(request.UserName, ip);

                return Results.Ok(new LoginResponse(
                    Success: true,
                    Token: token,
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
                        Success: false, Token: null, ExpiresAt: null,
                        Error: "Çok fazla başarısız deneme. Hesabınız 15 dakika kilitlendi.",
                        AttemptsRemaining: 0,
                        LockedUntilUtc: DateTime.UtcNow.Add(failure.LockoutDuration ?? TimeSpan.FromMinutes(15))),
                        statusCode: StatusCodes.Status423Locked);
                }

                return Results.Json(new LoginResponse(
                    Success: false, Token: null, ExpiresAt: null,
                    Error: $"Giriş bilgileri hatalı. {failure.AttemptsRemaining} deneme hakkınız kaldı.",
                    AttemptsRemaining: failure.AttemptsRemaining,
                    LockedUntilUtc: null),
                    statusCode: StatusCodes.Status401Unauthorized);
            }
        })
        .WithName("Login")
        .WithSummary("JWT token ile kimlik doğrulama — brute force korumalı");

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
        .WithSummary("JWT token doğrulama — geçerlilik, userId, tenantId");
    }

    // ── Request / Response Records ──

    public record LoginRequest(string UserName, string Password);

    public record LoginResponse(
        bool Success, string? Token, DateTime? ExpiresAt, string? Error,
        int? AttemptsRemaining, DateTime? LockedUntilUtc);

    public record ValidateTokenRequest(string Token);
}
