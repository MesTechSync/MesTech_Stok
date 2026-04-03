using MediatR;
using MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;

namespace MesTech.WebApi.Endpoints;

public static class FeedPreviewEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/feeds")
            .WithTags("Feeds")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/feeds/{id}/preview — feed ön izleme
        group.MapPost("/{id:guid}/preview", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new PreviewFeedCommand(id), ct);
            return Results.Ok(result);
        })
        .WithName("PreviewFeed")
        .WithSummary("Feed kaynağını ön izle (ilk N ürün)").Produces(200).Produces(400)
        .CacheOutput("Dashboard30s");

        // POST /api/v1/feeds/{id}/import — feed'den ürün içe aktar
        group.MapPost("/{id:guid}/import", async (
            Guid id,
            ImportFromFeedRequest request,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ImportFromFeedCommand(id, request.SelectedSkus, request.PriceMultiplier), ct);
            return Results.Ok(result);
        })
        .WithName("ImportFromFeed")
        .WithSummary("Feed'den seçili ürünleri içe aktar").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }

    /// <summary>
    /// Request body for importing products from a feed.
    /// Route provides FeedSourceId; body provides selection criteria.
    /// </summary>
    public record ImportFromFeedRequest(List<string> SelectedSkus, decimal PriceMultiplier);
}
