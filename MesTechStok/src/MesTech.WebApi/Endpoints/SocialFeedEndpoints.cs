using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

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
        .WithName("GetAllFeedStatuses")
        .WithSummary("Tüm sosyal feed platform durumlarını listele");

        // POST /api/v1/social-feeds/{platform}/generate — feed üretimini tetikle
        group.MapPost("/{platform}/generate", async (
            string platform,
            FeedGenerationRequest request,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.BadRequest(new { error = $"Bilinmeyen platform: {platform}" });

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.NotFound(new { error = $"Platform adaptörü bulunamadı: {platform}" });

            var result = await adapter.GenerateFeedAsync(request, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.Errors });
        })
        .WithName("GenerateFeed")
        .WithSummary("Belirtilen platform için feed üretimini tetikle");

        // GET /api/v1/social-feeds/{platform}/status — platform feed durumu
        group.MapGet("/{platform}/status", async (
            string platform,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.BadRequest(new { error = $"Bilinmeyen platform: {platform}" });

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.NotFound(new { error = $"Platform adaptörü bulunamadı: {platform}" });

            var status = await adapter.GetFeedStatusAsync(ct);
            return Results.Ok(status);
        })
        .WithName("GetFeedStatus")
        .WithSummary("Belirtilen platform feed durumunu sorgula");

        // POST /api/v1/social-feeds/{platform}/validate — feed URL doğrula
        group.MapPost("/{platform}/validate", async (
            string platform,
            FeedValidateRequest request,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.BadRequest(new { error = $"Bilinmeyen platform: {platform}" });

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.NotFound(new { error = $"Platform adaptörü bulunamadı: {platform}" });

            var result = await adapter.ValidateFeedAsync(request.FeedUrl, ct);
            return Results.Ok(result);
        })
        .WithName("ValidateFeed")
        .WithSummary("Feed URL'ini platform validator üzerinden doğrula");

        // POST /api/v1/social-feeds/{platform}/schedule — otomatik yenileme zamanla
        group.MapPost("/{platform}/schedule", async (
            string platform,
            FeedScheduleRequest request,
            IEnumerable<ISocialFeedAdapter> adapters,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SocialFeedPlatform>(platform, ignoreCase: true, out var parsedPlatform))
                return Results.BadRequest(new { error = $"Bilinmeyen platform: {platform}" });

            var adapter = adapters.FirstOrDefault(a => a.Platform == parsedPlatform);
            if (adapter is null)
                return Results.NotFound(new { error = $"Platform adaptörü bulunamadı: {platform}" });

            await adapter.ScheduleRefreshAsync(request.Interval, ct);
            return Results.NoContent();
        })
        .WithName("ScheduleFeedRefresh")
        .WithSummary("Platform feed otomatik yenileme aralığını ayarla");
    }

    /// <summary>Feed URL doğrulama istek gövdesi.</summary>
    public record FeedValidateRequest(string FeedUrl);

    /// <summary>Feed zamanlama istek gövdesi.</summary>
    public record FeedScheduleRequest(TimeSpan Interval);
}
