using MediatR;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;
using MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;

namespace MesTech.WebApi.Endpoints;

public static class ProductFetchEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/products/fetch-from-url — URL'den ürün bilgisi çek
        group.MapPost("/fetch-from-url", async (
            FetchProductFromUrlCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("FetchProductFromUrl")
        .WithSummary("URL'den ürün bilgisi çek (scrape/API)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/products/fetch-from-platform — platform'dan ürün çek (G564)
        group.MapPost("/fetch-from-platform", async (
            FetchProductFromPlatformQuery query,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("FetchProductFromPlatform")
        .WithSummary("Platform URL'sinden ürün bilgisi scrape et")
        .Produces(200).Produces(404)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
