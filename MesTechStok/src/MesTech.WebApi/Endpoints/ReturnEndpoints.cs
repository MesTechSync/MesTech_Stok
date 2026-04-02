using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class ReturnEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/returns")
            .WithTags("Returns")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/returns — iade listesi (G-TUR31 menu mapping)
        group.MapGet("/", async (
            Guid tenantId, int count = 100,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetReturnListQuery(tenantId, Math.Clamp(count, 1, 200)), ct);
            return Results.Ok(result);
        })
        .WithName("GetReturnList")
        .WithSummary("İade listesi — durum, tutar, tarih bilgisiyle")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/returns/{id} — iade detay (G562-DEV6)
        group.MapGet("/{id:guid}", async (
            Guid id,
            MesTech.Domain.Interfaces.IReturnRequestRepository repo,
            CancellationToken ct) =>
        {
            var returnRequest = await repo.GetByIdAsync(id);
            return returnRequest is null
                ? Results.NotFound()
                : Results.Ok(returnRequest);
        })
        .WithName("GetReturnById")
        .WithSummary("Iade detay — ID ile sorgulama")
        .Produces(200).Produces(404);

        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ApproveReturnCommand(id), ct);
            return Results.Ok(new ReturnApproveResponse(
                "Iade onaylandi — stok geri eklendi", id, DateTime.UtcNow));
        })
        .WithName("ApproveReturn")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Iade onay — stok geri + muhasebe ters kayit").Produces(200).Produces(400);

        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectReturnBody body,
            ISender mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new RejectReturnCommand(id, body.Reason), ct);
            return Results.Ok(new StatusResponse("Rejected", body.Reason));
        })
        .WithName("RejectReturn")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Iade red — sebep zorunlu").Produces(200).Produces(400);
    }

    public record RejectReturnBody(string Reason);

    public sealed record ReturnApproveResponse(string Message, Guid ReturnId, DateTime ProcessedAt);
}
