using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformList;

public sealed class GetPlatformListHandler : IRequestHandler<GetPlatformListQuery, List<PlatformCardDto>>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IAdapterFactory _adapterFactory;

    private static readonly Dictionary<PlatformType, (string Name, string Color, string AuthType)> PlatformMeta = new()
    {
        [PlatformType.Trendyol] = ("Trendyol", "#FF6F00", "API Key+Secret"),
        [PlatformType.Hepsiburada] = ("Hepsiburada", "#FF6000", "Bearer Token"),
        [PlatformType.N11] = ("N11", "#0B2441", "SOAP API Key"),
        [PlatformType.Ciceksepeti] = ("Ciceksepeti", "#007A3D", "x-api-key"),
        [PlatformType.Pazarama] = ("Pazarama", "#6600CC", "OAuth2 CC"),
        [PlatformType.Amazon] = ("Amazon", "#FF9900", "OAuth2 LWA"),
        [PlatformType.eBay] = ("eBay", "#E53238", "OAuth2"),
        [PlatformType.Ozon] = ("Ozon", "#005BFF", "Token"),
        [PlatformType.PttAVM] = ("PttAVM", "#FFD100", "Token"),
        [PlatformType.OpenCart] = ("OpenCart", "#2EA1D9", "MySQL Direct"),
        [PlatformType.Etsy] = ("Etsy", "#F1641E", "OAuth2"),
        [PlatformType.Bitrix24] = ("Bitrix24", "#2FC6F6", "Webhook"),
        [PlatformType.AmazonEu] = ("Amazon EU", "#FF9900", "OAuth2 LWA"),
    };

    public GetPlatformListHandler(
        IStoreRepository storeRepository,
        IAdapterFactory adapterFactory)
    {
        _storeRepository = storeRepository;
        _adapterFactory = adapterFactory;
    }

    public async Task<List<PlatformCardDto>> Handle(
        GetPlatformListQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var allStores = await _storeRepository
            .GetByTenantIdAsync(request.TenantId, cancellationToken);

        var result = new List<PlatformCardDto>();

        foreach (PlatformType platform in Enum.GetValues<PlatformType>())
        {
            var meta = PlatformMeta.TryGetValue(platform, out var m)
                ? m
                : (Name: platform.ToString(), Color: "#888888", AuthType: "Unknown");

            var stores = allStores.Where(s => s.PlatformType == platform).ToList();
            var adapter = _adapterFactory.Resolve(platform);

            result.Add(new PlatformCardDto
            {
                Platform = platform,
                Name = meta.Name,
                LogoColor = meta.Color,
                AuthType = meta.AuthType,
                AdapterAvailable = adapter is not null,
                StoreCount = stores.Count,
                ActiveStoreCount = stores.Count(s => s.IsActive),
                WorstStatus = stores.Count == 0 ? null : "Unknown",
                LastSyncAt = null,
                TotalProducts = stores.Sum(s => s.ProductMappings?.Count ?? 0),
                TotalOrders = 0,
            });
        }

        return result;
    }
}
