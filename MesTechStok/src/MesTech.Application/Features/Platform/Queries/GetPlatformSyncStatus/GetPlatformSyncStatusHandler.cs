using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;

public sealed class GetPlatformSyncStatusHandler
    : IRequestHandler<GetPlatformSyncStatusQuery, List<PlatformSyncStatusDto>>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IAdapterFactory _adapterFactory;
    private readonly IPlatformHealthProvider? _healthProvider;
    private readonly ILogger<GetPlatformSyncStatusHandler> _logger;

    private static readonly Dictionary<PlatformType, string> PlatformNames = new()
    {
        [PlatformType.Trendyol] = "Trendyol",
        [PlatformType.Hepsiburada] = "Hepsiburada",
        [PlatformType.N11] = "N11",
        [PlatformType.Ciceksepeti] = "Ciceksepeti",
        [PlatformType.Pazarama] = "Pazarama",
        [PlatformType.Amazon] = "Amazon",
        [PlatformType.eBay] = "eBay",
        [PlatformType.Ozon] = "Ozon",
        [PlatformType.PttAVM] = "PttAVM",
        [PlatformType.OpenCart] = "OpenCart",
        [PlatformType.Etsy] = "Etsy",
        [PlatformType.Bitrix24] = "Bitrix24",
        [PlatformType.AmazonEu] = "Amazon EU",
    };

    public GetPlatformSyncStatusHandler(
        IStoreRepository storeRepository,
        IAdapterFactory adapterFactory,
        ILogger<GetPlatformSyncStatusHandler> logger,
        IPlatformHealthProvider? healthProvider = null)
    {
        _storeRepository = storeRepository;
        _adapterFactory = adapterFactory;
        _logger = logger;
        _healthProvider = healthProvider;
    }

    public async Task<List<PlatformSyncStatusDto>> Handle(
        GetPlatformSyncStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<Domain.Entities.Store> allStores;
        try
        {
            allStores = await _storeRepository
                .GetByTenantIdAsync(request.TenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB unavailable for PlatformSyncStatus — returning empty list");
            return new List<PlatformSyncStatusDto>();
        }

        var result = new List<PlatformSyncStatusDto>();

        foreach (PlatformType platform in Enum.GetValues<PlatformType>())
        {
            var stores = allStores.Where(s => s.PlatformType == platform).ToList();
            if (stores.Count == 0)
                continue;

            var platformName = PlatformNames.TryGetValue(platform, out var name)
                ? name
                : platform.ToString();

            // Compute health status based on store count and adapter availability
            var adapter = _adapterFactory.Resolve(platform);
            var healthStatus = "Healthy";
            var healthColor = "#28a745";

            if (adapter is null)
            {
                healthStatus = "Error";
                healthColor = "#dc3545";
            }
            else if (stores.All(s => !s.IsActive))
            {
                healthStatus = "Warning";
                healthColor = "#ffc107";
            }

            // Enrich with real health data from PlatformHealthHistory (if available)
            var health = _healthProvider?.GetHealthSummary(platformName.ToLowerInvariant())
                      ?? _healthProvider?.GetHealthSummary(platform.ToString().ToLowerInvariant());
            if (health is not null)
            {
                healthStatus = health.UptimePercent24h >= 95 ? "Healthy"
                    : health.UptimePercent24h >= 70 ? "Warning" : "Error";
                healthColor = healthStatus switch
                {
                    "Healthy" => "#28a745",
                    "Warning" => "#ffc107",
                    _ => "#dc3545"
                };
            }

            result.Add(new PlatformSyncStatusDto
            {
                Platform = platform,
                PlatformName = platformName,
                StoreCount = stores.Count,
                LastSyncAt = health?.LastCheckUtc,
                LastSuccessAt = health is { FailedChecks24h: 0 } ? health.LastCheckUtc : null,
                LastError = health is { FailedChecks24h: > 0 } ? $"{health.FailedChecks24h} hata (24s)" : null,
                ErrorCountToday = health?.FailedChecks24h ?? 0,
                HealthStatus = healthStatus,
                HealthColor = healthColor
            });
        }

        return result;
    }
}
