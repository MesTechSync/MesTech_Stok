using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Queries.GetStoresByTenant;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Admin-only store management endpoints.
/// GET / wires to existing GetStoresByTenantQuery.
/// POST / and POST /{id}/test-connection are DEV1-DEPENDENCY stubs.
/// </summary>
public static class StoreEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/stores")
            .WithTags("Stores (Admin)")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/admin/stores — kiracıya ait mağaza listesi
        group.MapGet("/", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetStoresByTenantQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetStores")
        .WithSummary("Kiracıya ait mağaza listesi")
        .CacheOutput("Lookup60s");

        // POST /api/v1/admin/stores — yeni mağaza oluştur
        group.MapPost("/", async (
            CreateStoreCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/stores/{result.StoreId}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(result.StoreId ?? Guid.Empty)))
                : Results.BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Mağaza oluşturulamadı"));
        })
        .WithName("CreateStore")
        .WithSummary("Yeni mağaza oluştur — admin only")
        .AddEndpointFilter<Filters.StorePlanLimitFilter>();

        // POST /api/v1/admin/stores/{id}/test-connection — mağaza API bağlantı testi
        group.MapPost("/{id:guid}/test-connection", async (
            Guid id,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TestStoreConnectionCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("TestStoreConnection")
        .WithSummary("Mağaza API bağlantı testi");
    }
}
