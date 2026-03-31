using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Headless test scope — IAdapterFactory implementasyonu.
/// Her platform için InMemoryPlatformAdapter döndürür.
/// Gerçek API çağrısı yapılmaz, sabit veri döner.
/// </summary>
public sealed class InMemoryAdapterFactory : IAdapterFactory
{
    private readonly Dictionary<string, InMemoryPlatformAdapter> _adapters;

    public InMemoryAdapterFactory()
    {
        var platformCodes = new[]
        {
            "Trendyol", "Hepsiburada", "Ciceksepeti", "N11", "Pazarama",
            "Amazon", "AmazonEu", "eBay", "Shopify", "WooCommerce",
            "Ozon", "Etsy", "Zalando", "PttAVM", "OpenCart", "Bitrix24"
        };

        _adapters = platformCodes.ToDictionary(
            code => code,
            code => new InMemoryPlatformAdapter(code),
            StringComparer.OrdinalIgnoreCase);
    }

    public IIntegratorAdapter? Resolve(PlatformType platformType)
        => Resolve(platformType.ToString());

    public IIntegratorAdapter? Resolve(string platformCode)
        => _adapters.TryGetValue(platformCode, out var adapter) ? adapter : null;

    public IReadOnlyList<IIntegratorAdapter> GetAll()
        => _adapters.Values.ToList<IIntegratorAdapter>().AsReadOnly();

    public T? ResolveCapability<T>(string platformCode) where T : class
        => Resolve(platformCode) as T;
}
