using MediatR;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;

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
        .WithSummary("URL'den ürün bilgisi çek (scrape/API)");
    }
}
