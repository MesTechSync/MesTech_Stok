using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Public demo mode endpoint — creates a temporary demo tenant (30-min TTL).
/// Rate-limited: AuthRateLimit policy (20 req/min per IP).
/// No authentication required — this is the onboarding funnel entry point.
/// </summary>
public static class DemoEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/demo")
            .WithTags("Demo")
            .RequireRateLimiting("AuthRateLimit");

        group.MapPost("/start", async (
            IDemoModeService demoService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            try
            {
                var session = await demoService.CreateDemoSessionAsync(ct);

                return Results.Ok(ApiResponse<DemoSessionResult>.Ok(session));
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("DemoEndpoints").LogError(ex, "[DemoMode] Failed to create demo session");
                return Results.Problem(
                    detail: "Demo oturumu oluşturulamadı. Lütfen daha sonra tekrar deneyin.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Demo session failed");
            }
        })
        .WithName("StartDemoSession")
        .WithSummary("30 dakikalık demo oturumu başlat — ücretsiz, kayıt gerektirmez")
        .Produces<ApiResponse<DemoSessionResult>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status429TooManyRequests)
        .Produces(StatusCodes.Status500InternalServerError)
        .AllowAnonymous()
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
