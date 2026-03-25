using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Factory;

/// <summary>
/// Kargo adapter fabrikasi — CargoProvider enum ile adapter resolve eder.
/// AdapterFactory pattern'ini takip eder.
/// </summary>
public sealed class CargoProviderFactory : ICargoProviderFactory
{
    private readonly Dictionary<CargoProvider, ICargoAdapter> _adapters;
    private readonly ILogger<CargoProviderFactory> _logger;

    public CargoProviderFactory(
        IEnumerable<ICargoAdapter> adapters,
        ILogger<CargoProviderFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _adapters = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
            .ToDictionary(a => a.Provider, a => a);

        _logger.LogInformation("CargoProviderFactory initialized with {Count} adapters: [{Providers}]",
            _adapters.Count, string.Join(", ", _adapters.Keys));
    }

    public ICargoAdapter? Resolve(CargoProvider provider)
    {
        _adapters.TryGetValue(provider, out var adapter);
        if (adapter is null)
            _logger.LogWarning("CargoProviderFactory: No adapter found for provider '{Provider}'", provider);
        return adapter;
    }

    public IReadOnlyList<ICargoAdapter> GetAll()
        => _adapters.Values.ToList().AsReadOnly();
}
