using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using MesTech.Application.Features.Onboarding.Commands.StartOnboarding;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;
using MesTech.Application.Features.Onboarding.Queries.GetV5ReadinessCheck;

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
        .WithSummary("Kurulum ilerleme durumu").Produces(200).Produces(400);

        // POST /api/v1/onboarding/start — start onboarding
        group.MapPost("/start", async (
            ISender mediator,
            StartOnboardingCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/onboarding/progress", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("StartOnboarding")
        .WithSummary("Kurulum sürecini başlat").Produces(200).Produces(400);

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
        .WithSummary("Kurulum adımını tamamla").Produces(200).Produces(400);

        // GET /api/v1/onboarding/v5-readiness — V5 özellik hazırlık kontrolü
        group.MapGet("/v5-readiness", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetV5ReadinessCheckQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetV5ReadinessCheck")
        .WithSummary("V5 özellik hazırlık kontrolü — ERP, Fulfillment, Komisyon, Cari, Raporlama").Produces(200).Produces(400);

        // POST /api/v1/onboarding/register — tam kayıt (tenant + admin + trial + onboarding)
        group.MapPost("/register", async (
            ISender mediator,
            RegisterTenantCommand command,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/onboarding/progress", result);
        })
        .WithName("RegisterTenant")
        .WithSummary("Yeni tenant kaydı — firma + admin kullanıcı + 14 gün trial + onboarding")
        .AllowAnonymous(); // Kayıt endpoint'i auth gerektirmez
    }
}
