using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;

public class GetPlatformSyncStatusHandler
    : IRequestHandler<GetPlatformSyncStatusQuery, List<PlatformSyncStatusDto>>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IAdapterFactory _adapterFactory;

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
        IAdapterFactory adapterFactory)
    {
        _storeRepository = storeRepository;
        _adapterFactory = adapterFactory;
    }

    public async Task<List<PlatformSyncStatusDto>> Handle(
        GetPlatformSyncStatusQuery request,
        CancellationToken cancellationToken)
    {
        var allStores = await _storeRepository
            .GetByTenantIdAsync(request.TenantId, cancellationToken);

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

            result.Add(new PlatformSyncStatusDto
            {
                Platform = platform,
                PlatformName = platformName,
                StoreCount = stores.Count,
                LastSyncAt = null, // Requires sync log — placeholder
                LastSuccessAt = null,
                LastError = null,
                ErrorCountToday = 0,
                HealthStatus = healthStatus,
                HealthColor = healthColor
            });
        }

        return result;
    }
}
