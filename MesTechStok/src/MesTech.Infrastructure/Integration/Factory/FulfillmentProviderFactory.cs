using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Factory;

/// <summary>
/// Default implementation: dictionary-based lookup, O(1) resolution.
/// </summary>
public sealed class FulfillmentProviderFactory : IFulfillmentProviderFactory
{
    private readonly Dictionary<FulfillmentCenter, IFulfillmentProvider> _providers;
    private readonly ILogger<FulfillmentProviderFactory> _logger;

    public FulfillmentProviderFactory(
        IEnumerable<IFulfillmentProvider> providers,
        ILogger<FulfillmentProviderFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(providers);

        _providers = new Dictionary<FulfillmentCenter, IFulfillmentProvider>();

        foreach (var provider in providers)
        {
            if (_providers.TryGetValue(provider.Center, out var existing))
            {
                _logger.LogWarning(
                    "[FulfillmentFactory] Duplicate provider for center {Center}: {Existing} overridden by {New}",
                    provider.Center, existing.GetType().Name, provider.GetType().Name);
            }

            _providers[provider.Center] = provider;
        }

        _logger.LogInformation(
            "[FulfillmentFactory] Initialized with {Count} provider(s): [{Centers}]",
            _providers.Count,
            string.Join(", ", _providers.Keys.Select(c => c.ToString())));
    }

    public IFulfillmentProvider? Resolve(FulfillmentCenter center)
    {
        if (_providers.TryGetValue(center, out var provider))
            return provider;

        _logger.LogWarning("[FulfillmentFactory] No provider registered for center {Center}", center);
        return null;
    }

    public IReadOnlyList<IFulfillmentProvider> GetAll()
        => _providers.Values.ToList().AsReadOnly();
}
