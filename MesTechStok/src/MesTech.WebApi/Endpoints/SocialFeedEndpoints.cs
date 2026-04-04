using MediatR;
using MesTech.Application.Features.SocialFeed.Commands.RefreshSocialFeed;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class SocialFeedEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/social-feeds")
            .WithTags("SocialFeeds")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/social-feeds — tüm platform adaptörlerinin feed durumu
        group.MapGet("/", async (
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            var results = new List<object>();
            foreach (var adapter in adapters)
            {
                var status = await adapter.GetFeedStatusAsync(ct);
                results.Add(new
                {
                    platform = adapter.Platform.ToString(),
                    lastGenerated = status.LastGenerated,
                    itemCount = status.ItemCount,
                    nextScheduled = status.NextScheduled,
                    isHealthy = status.IsHealthy
                });
            }
            return Results.Ok(results);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetAllFeedStatuses")
        .WithSummary("Tüm sosyal feed platform durumlarını listele").Produces(200).Produces(400);

        // POST /api/v1/social-feeds/{platform}/generate — feed üretimini tetikle
        group.MapPost("/{platform}/generate", async (
            string platform,
            FeedGenerationRequest request,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.Problem(detail: $"Bilinmeyen platform: {platform}", statusCode: 400);

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.Problem(detail: $"Platform adaptörü bulunamadı: {platform}", statusCode: 404);

            var result = await adapter.GenerateFeedAsync(request, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(detail: string.Join("; ", result.Errors ?? Array.Empty<string>()), statusCode: 422);
        })
        .WithName("GenerateFeed")
        .WithSummary("Belirtilen platform için feed üretimini tetikle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/social-feeds/{platform}/status — platform feed durumu
        group.MapGet("/{platform}/status", async (
            string platform,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.Problem(detail: $"Bilinmeyen platform: {platform}", statusCode: 400);

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.Problem(detail: $"Platform adaptörü bulunamadı: {platform}", statusCode: 404);

            var status = await adapter.GetFeedStatusAsync(ct);
            return Results.Ok(status);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetFeedStatus")
        .WithSummary("Belirtilen platform feed durumunu sorgula").Produces(200).Produces(400);

        // POST /api/v1/social-feeds/{platform}/validate — feed URL doğrula
        group.MapPost("/{platform}/validate", async (
            string platform,
            FeedValidateRequest request,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.Problem(detail: $"Bilinmeyen platform: {platform}", statusCode: 400);

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.Problem(detail: $"Platform adaptörü bulunamadı: {platform}", statusCode: 404);

            var result = await adapter.ValidateFeedAsync(request.FeedUrl, ct);
            return Results.Ok(result);
        })
        .WithName("ValidateFeed")
        .WithSummary("Feed URL'ini platform validator üzerinden doğrula").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/social-feeds/{platform}/schedule — otomatik yenileme zamanla
        group.MapPost("/{platform}/schedule", async (
            string platform,
            FeedScheduleRequest request,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.Problem(detail: $"Bilinmeyen platform: {platform}", statusCode: 400);

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.Problem(detail: $"Platform adaptörü bulunamadı: {platform}", statusCode: 404);

            await adapter.ScheduleRefreshAsync(request.Interval, ct);
            return Results.NoContent();
        })
        .WithName("ScheduleFeedRefresh")
        .WithSummary("Platform feed otomatik yenileme aralığını ayarla").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/social-feeds/refresh/{configId} — feed yenileme tetikle
        group.MapPost("/refresh/{configId:guid}", async (
            Guid configId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefreshSocialFeedCommand(configId), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("RefreshSocialFeed")
        .WithSummary("Sosyal feed yenileme tetikle — Google Merchant, Facebook Shop vb.")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }

    /// <summary>Feed URL doğrulama istek gövdesi.</summary>
    public record FeedValidateRequest(string FeedUrl);

    /// <summary>Feed zamanlama istek gövdesi.</summary>
    public record FeedScheduleRequest(TimeSpan Interval);
}
