using MediatR;
using MesTech.Application.Features.Health.Queries.GetMesaStatus;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// MESA OS bridge health endpoint — AI/Bot servis durumu.
/// DEV6 TUR13: Ayrı dosya (linter workaround).
/// </summary>
public static class MesaStatusEndpoint
{
    public static void Map(WebApplication app)
    {
        // GET /health/mesa — MESA OS AI/Bot bridge durumu
        app.MapGet("/health/mesa", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMesaStatusQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetMesaStatus")
        .WithSummary("MESA OS bridge bağlantı durumu — AI/Bot servis health")
        .WithTags("Health")
        .RequireRateLimiting("HealthRateLimit");
    }
}
