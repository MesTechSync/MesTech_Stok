using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Authentication endpoints for Blazor SaaS login (Dalga 9).
/// POST /api/v1/auth/login — AllowAnonymous (bypasses API key middleware).
/// </summary>
public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // POST /api/v1/auth/login — authenticate user and return JWT
        group.MapPost("/login", (LoginRequest request, IJwtTokenService jwtService) =>
        {
            // Phase 1: Placeholder validation — will be replaced with real
            // BCrypt password check against User entity in Dalga 9 Task 5.
            // For now, reject empty credentials and return a token for valid-looking requests.
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new LoginResponse(
                    Success: false,
                    Token: null,
                    ExpiresAt: null,
                    Error: "UserName and Password are required."));
            }

            // TODO(Dalga9-Task5): Real user lookup + BCrypt.Verify against hashed password
            // TODO(Dalga9-Task5): Extract real userId, tenantId, roles from User entity
            // Placeholder: generate token with deterministic dev IDs (NEVER in production)
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var roles = new[] { "User" };

            try
            {
                var token = jwtService.GenerateToken(userId, tenantId, request.UserName, roles);
                var expiresAt = DateTime.UtcNow.AddMinutes(480);

                return Results.Ok(new LoginResponse(
                    Success: true,
                    Token: token,
                    ExpiresAt: expiresAt,
                    Error: null));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Token generation failed");
            }
        });

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
        });
    }

    // ── Request / Response Records ──

    public record LoginRequest(string UserName, string Password);

    public record LoginResponse(bool Success, string? Token, DateTime? ExpiresAt, string? Error);

    public record ValidateTokenRequest(string Token);
}
