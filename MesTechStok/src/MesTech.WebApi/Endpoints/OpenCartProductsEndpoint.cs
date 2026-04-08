using MediatR;
using MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// OpenCart platform ürün listesi endpoint.
/// DEV6 TUR15: G519 — handler-endpoint gap kapatma.
/// </summary>
public static class OpenCartProductsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platforms/opencart")
            .WithTags("Platforms")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/platforms/opencart/products — OpenCart ürünleri (sayfalı)
        group.MapGet("/products", async (
            Guid tenantId,
            Guid storeId,
            int? page,
            int? pageSize,
            string? searchTerm,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetOpenCartProductsQuery(
                    tenantId, storeId,
                    page ?? 1, Math.Clamp(pageSize ?? 50, 1, 200),
                    searchTerm), ct);
            return Results.Ok(result);
        })
        .WithName("GetOpenCartProductsPaged")
        .WithSummary("OpenCart platform ürün listesi — sayfalı arama destekli")
        .Produces<GetOpenCartProductsResult>(200);
    }
}
