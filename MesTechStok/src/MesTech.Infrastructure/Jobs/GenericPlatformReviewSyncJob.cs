using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tum IReviewCapableAdapter platformlari icin periyodik review sync.
/// Platform kodu parametre olarak alinir — her platform ayri Hangfire recurring job.
/// Cevapsiz review'lar icin ProductReviewReceivedIntegrationEvent publish eder.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class GenericPlatformReviewSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GenericPlatformReviewSyncJob> _logger;

    public GenericPlatformReviewSyncJob(
        IAdapterFactory factory,
        ITenantProvider tenantProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<GenericPlatformReviewSyncJob> logger)
    {
        _factory = factory;
        _tenantProvider = tenantProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _logger.LogInformation("[ReviewSync] {Platform} review sync basliyor... TenantId={TenantId}", platformCode, tenantId);

        var adapter = _factory.ResolveCapability<IReviewCapableAdapter>(platformCode);
        if (adapter is null)
        {
            _logger.LogDebug("[ReviewSync] {Platform} IReviewCapableAdapter bulunamadi — skip", platformCode);
            return;
        }

        try
        {
            int page = 0;
            int totalFetched = 0;
            int totalUnreplied = 0;
            const int pageSize = 50;

            while (!ct.IsCancellationRequested)
            {
                var reviews = await adapter.GetProductReviewsAsync(page, pageSize, ct).ConfigureAwait(false);
                if (reviews.Count == 0) break;

                totalFetched += reviews.Count;

                foreach (var review in reviews.Where(r => !r.IsReplied))
                {
                    totalUnreplied++;
                    await _publishEndpoint.Publish(new ProductReviewReceivedIntegrationEvent(
                        ReviewId: review.Id,
                        ProductId: review.ProductId,
                        Rating: review.Rate,
                        Comment: review.Comment,
                        IsReplied: false,
                        PlatformCode: platformCode,
                        OccurredAt: DateTime.UtcNow), ct).ConfigureAwait(false);
                }

                if (reviews.Count < pageSize) break;
                page++;
            }

            _logger.LogInformation(
                "[ReviewSync] {Platform} tamamlandi: {Total} review, {Unreplied} cevapsiz event",
                platformCode, totalFetched, totalUnreplied);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[ReviewSync] {Platform} review sync HATA", platformCode);
            throw;
        }
    }
}
