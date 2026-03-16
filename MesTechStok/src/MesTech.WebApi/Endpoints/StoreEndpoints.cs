using MediatR;
using MesTech.Application.Queries.GetStoresByTenant;

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
        .WithSummary("Kiracıya ait mağaza listesi");

        // POST /api/v1/admin/stores — yeni mağaza oluştur
        // DEV1-DEPENDENCY: CreateStoreCommand not yet available
        group.MapPost("/", () =>
            Results.Accepted("/api/v1/admin/stores", new
            {
                Message = "Create store endpoint — DEV1 CreateStoreCommand pending",
                Status = "not_implemented"
            }))
        .WithName("CreateStore")
        .WithSummary("Yeni mağaza oluştur — admin only (DEV1-DEPENDENCY)");

        // POST /api/v1/admin/stores/{id}/test-connection — mağaza API bağlantı testi
        // DEV1-DEPENDENCY: TestStoreConnectionCommand not yet available
        group.MapPost("/{id:guid}/test-connection", (Guid id) =>
            Results.Ok(new
            {
                Message = "Store connection test endpoint — DEV1 TestStoreConnectionCommand pending",
                StoreId = id,
                Status = "not_implemented"
            }))
        .WithName("TestStoreConnection")
        .WithSummary("Mağaza API bağlantı testi (DEV1-DEPENDENCY)");
    }
}
