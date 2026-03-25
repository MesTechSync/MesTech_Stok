using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Factory;

/// <summary>
/// DI'dan gelen tum IIntegratorAdapter'lari dictionary'de tutar.
/// PlatformType enum veya string ile hizli lookup.
/// </summary>
public sealed class AdapterFactory : IAdapterFactory
{
    private readonly Dictionary<string, IIntegratorAdapter> _adapters;
    private readonly ILogger<AdapterFactory> _logger;

    public AdapterFactory(
        IEnumerable<IIntegratorAdapter> adapters,
        ILogger<AdapterFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _adapters = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
            .ToDictionary(a => a.PlatformCode, a => a, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("AdapterFactory initialized with {Count} adapters: [{Codes}]",
            _adapters.Count, string.Join(", ", _adapters.Keys));
    }

    public IIntegratorAdapter? Resolve(PlatformType platformType)
        => Resolve(platformType.ToString());

    public IIntegratorAdapter? Resolve(string platformCode)
    {
        _adapters.TryGetValue(platformCode, out var adapter);
        if (adapter is null)
            _logger.LogWarning("AdapterFactory: No adapter found for platform '{Code}'", platformCode);
        return adapter;
    }

    public IReadOnlyList<IIntegratorAdapter> GetAll()
        => _adapters.Values.ToList().AsReadOnly();

    public T? ResolveCapability<T>(string platformCode) where T : class
    {
        var adapter = Resolve(platformCode);
        if (adapter is T capable)
            return capable;

        if (adapter is not null)
            _logger.LogWarning("Adapter '{Code}' does not implement {Capability}",
                platformCode, typeof(T).Name);
        return null;
    }
}
