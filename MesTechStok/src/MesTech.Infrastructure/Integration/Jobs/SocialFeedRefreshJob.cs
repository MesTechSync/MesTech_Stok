using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Jobs;

/// <summary>
/// Hangfire recurring job: refreshes all active social feed configurations every 6 hours.
/// For each active <see cref="SocialFeedConfiguration"/>, resolves the matching
/// <see cref="ISocialFeedAdapter"/> and calls <see cref="ISocialFeedAdapter.GenerateFeedAsync"/>.
/// Throttle: max 1 feed generation per 30 seconds to avoid API/storage hammering.
/// </summary>
public class SocialFeedRefreshJob
{
    private readonly AppDbContext _dbContext;
    private readonly IEnumerable<ISocialFeedAdapter> _adapters;
    private readonly ILogger<SocialFeedRefreshJob> _logger;

    private static readonly TimeSpan ThrottleDelay = TimeSpan.FromSeconds(30);

    public SocialFeedRefreshJob(
        AppDbContext dbContext,
        IEnumerable<ISocialFeedAdapter> adapters,
        ILogger<SocialFeedRefreshJob> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[SocialFeedRefresh] Starting feed refresh cycle at {Time}", DateTime.UtcNow);

        var configs = await _dbContext.Set<SocialFeedConfiguration>()
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        if (configs.Count == 0)
        {
            _logger.LogInformation("[SocialFeedRefresh] No active feed configurations found.");
            return;
        }

        _logger.LogInformation("[SocialFeedRefresh] Processing {Count} active feed configuration(s).", configs.Count);

        var adapterMap = _adapters.ToDictionary(a => a.Platform);

        var lastRunAt = DateTime.MinValue;

        foreach (var config in configs)
        {
            ct.ThrowIfCancellationRequested();

            // Throttle: enforce 30-second gap between consecutive generations
            var elapsed = DateTime.UtcNow - lastRunAt;
            if (lastRunAt != DateTime.MinValue && elapsed < ThrottleDelay)
            {
                var wait = ThrottleDelay - elapsed;
                _logger.LogDebug("[SocialFeedRefresh] Throttling — waiting {Wait}ms before next feed", wait.TotalMilliseconds);
                await Task.Delay(wait, ct);
            }

            await RefreshSingleFeedAsync(config, adapterMap, ct);
            lastRunAt = DateTime.UtcNow;
        }

        _logger.LogInformation("[SocialFeedRefresh] Feed refresh cycle completed at {Time}", DateTime.UtcNow);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task RefreshSingleFeedAsync(
        SocialFeedConfiguration config,
        Dictionary<SocialFeedPlatform, ISocialFeedAdapter> adapterMap,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[SocialFeedRefresh] Processing config {ConfigId} platform={Platform} tenant={TenantId}",
            config.Id, config.Platform, config.TenantId);

        if (!adapterMap.TryGetValue(config.Platform, out var adapter))
        {
            var msg = $"No adapter registered for platform {config.Platform}.";
            _logger.LogWarning("[SocialFeedRefresh] {Message} Config={ConfigId}", msg, config.Id);
            config.RecordError(msg);
            await _dbContext.SaveChangesAsync(ct);
            return;
        }

        try
        {
            var categoryFilter = string.IsNullOrWhiteSpace(config.CategoryFilter)
                ? null
                : config.CategoryFilter
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            var request = new FeedGenerationRequest(
                StoreId: config.TenantId,
                CategoryFilter: categoryFilter?.AsReadOnly(),
                Currency: "TRY",
                Language: "tr");

            var result = await adapter.GenerateFeedAsync(request, ct);

            if (result.Success)
            {
                config.RecordGeneration(
                    feedUrl: result.FeedUrl ?? string.Empty,
                    itemCount: result.ItemCount,
                    error: result.Errors is { Count: > 0 }
                        ? string.Join("; ", result.Errors)
                        : null);

                _logger.LogInformation(
                    "[SocialFeedRefresh] Feed generated for config {ConfigId}: {Count} items, url={Url}",
                    config.Id, result.ItemCount, result.FeedUrl);
            }
            else
            {
                var error = result.Errors is { Count: > 0 }
                    ? string.Join("; ", result.Errors)
                    : "Feed generation failed (no error detail)";

                config.RecordError(error);

                _logger.LogWarning(
                    "[SocialFeedRefresh] Feed generation failed for config {ConfigId}: {Error}",
                    config.Id, error);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SocialFeedRefresh] Unexpected error for config {ConfigId}", config.Id);
            config.RecordError(ex.Message);
        }
        finally
        {
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Registers the SocialFeedRefreshJob as a Hangfire recurring job (every 6 hours).
    /// Call this from HangfireConfig.RegisterRecurringJobs().
    /// </summary>
    public static void Register()
    {
        RecurringJob.AddOrUpdate<SocialFeedRefreshJob>(
            "social-feed-refresh",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 */6 * * *"); // every 6 hours
    }
}
