using MediatR;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Onboarding.Commands.StartOnboarding;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;

namespace MesTech.WebApi.Endpoints;

public static class OnboardingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/onboarding")
            .WithTags("Onboarding")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/onboarding/progress — onboarding progress
        group.MapGet("/progress", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetOnboardingProgressQuery(tenantId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetOnboardingProgress")
        .WithSummary("Kurulum ilerleme durumu");

        // POST /api/v1/onboarding/start — start onboarding
        group.MapPost("/start", async (
            ISender mediator,
            StartOnboardingCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/onboarding/progress", new { id });
        })
        .WithName("StartOnboarding")
        .WithSummary("Kurulum sürecini başlat");

        // POST /api/v1/onboarding/complete-step — complete onboarding step
        group.MapPost("/complete-step", async (
            ISender mediator,
            CompleteOnboardingStepCommand command,
            CancellationToken ct = default) =>
        {
            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("CompleteOnboardingStep")
        .WithSummary("Kurulum adımını tamamla");
    }
}
