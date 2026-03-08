using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Factory;

/// <summary>
/// Siparis icin en uygun kargo firmasini secer.
/// Dalga 3: sirayla dene, ilk musait olan. Dalga 4: fiyat + bolge + AI.
/// </summary>
public class CargoProviderSelector : ICargoProviderSelector
{
    private readonly ICargoProviderFactory _factory;
    private readonly ILogger<CargoProviderSelector> _logger;

    // Oncelik sirasi — tenant bazli config Dalga 4'te
    private static readonly CargoProvider[] Priority =
    {
        CargoProvider.YurticiKargo,
        CargoProvider.ArasKargo,
        CargoProvider.SuratKargo
    };

    public CargoProviderSelector(
        ICargoProviderFactory factory,
        ILogger<CargoProviderSelector> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CargoProvider> SelectBestProviderAsync(Order order, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        foreach (var provider in Priority)
        {
            var adapter = _factory.Resolve(provider);
            if (adapter is null) continue;

            try
            {
                if (await adapter.IsAvailableAsync(ct).ConfigureAwait(false))
                {
                    _logger.LogInformation("Selected cargo provider {Provider} for order {OrderId}",
                        provider, order.Id);
                    return provider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cargo provider {Provider} availability check failed", provider);
            }
        }

        _logger.LogWarning("No available cargo provider found for order {OrderId}, defaulting to YurticiKargo", order.Id);
        return CargoProvider.YurticiKargo;
    }
}
