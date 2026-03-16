using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Factory;

/// <summary>
/// Siparis icin en uygun kargo firmasini secer.
/// Phase C: 3 strateji destegi — AvailabilityFirst, CheapestFirst, FastestFirst.
/// </summary>
public class CargoProviderSelector : ICargoProviderSelector
{
    private readonly ICargoProviderFactory _factory;
    private readonly ILogger<CargoProviderSelector> _logger;

    // Oncelik sirasi — tenant bazli config Dalga 4'te
    // K1d-04: Tum 7 kargo saglayici destekleniyor (UPS haric — yurtici odak)
    private static readonly CargoProvider[] Priority =
    {
        CargoProvider.YurticiKargo,
        CargoProvider.ArasKargo,
        CargoProvider.SuratKargo,
        CargoProvider.MngKargo,
        CargoProvider.PttKargo,
        CargoProvider.Hepsijet,
        CargoProvider.Sendeo
    };

    public CargoProviderSelector(
        ICargoProviderFactory factory,
        ILogger<CargoProviderSelector> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<CargoProvider> SelectBestProviderAsync(Order order, CancellationToken ct = default)
        => SelectBestProviderAsync(order, CargoSelectionStrategy.AvailabilityFirst, null, ct);

    /// <inheritdoc />
    public async Task<CargoProvider> SelectBestProviderAsync(
        Order order,
        CargoSelectionStrategy strategy,
        ShipmentRequest? shipmentRequest = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        return strategy switch
        {
            CargoSelectionStrategy.CheapestFirst => await SelectByRateAsync(order, shipmentRequest, sortByPrice: true, ct).ConfigureAwait(false),
            CargoSelectionStrategy.FastestFirst => await SelectByRateAsync(order, shipmentRequest, sortByPrice: false, ct).ConfigureAwait(false),
            _ => await SelectByAvailabilityAsync(order, ct).ConfigureAwait(false)
        };
    }

    /// <summary>
    /// Mevcut davranis: oncelik sirasina gore ilk musait olan.
    /// </summary>
    private async Task<CargoProvider> SelectByAvailabilityAsync(Order order, CancellationToken ct)
    {
        foreach (var provider in Priority)
        {
            var adapter = _factory.Resolve(provider);
            if (adapter is null) continue;

            try
            {
                if (await adapter.IsAvailableAsync(ct).ConfigureAwait(false))
                {
                    _logger.LogInformation("Selected cargo provider {Provider} for order {OrderId} (AvailabilityFirst)",
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

    /// <summary>
    /// Tum adaptorlerden fiyat sorgusu yapar, en ucuz veya en hizli secimi dondurur.
    /// ICargoRateProvider implement eden adaptor yoksa AvailabilityFirst fallback.
    /// </summary>
    private async Task<CargoProvider> SelectByRateAsync(
        Order order,
        ShipmentRequest? shipmentRequest,
        bool sortByPrice,
        CancellationToken ct)
    {
        var allAdapters = _factory.GetAll();

        // Build shipment request from order if not explicitly provided
        var request = shipmentRequest ?? BuildDefaultShipmentRequest(order);

        var rateResults = new List<CargoRateResult>();

        foreach (var adapter in allAdapters)
        {
            if (adapter is not ICargoRateProvider rateProvider) continue;

            try
            {
                if (!await adapter.IsAvailableAsync(ct).ConfigureAwait(false))
                    continue;

                var rate = await rateProvider.GetRateAsync(request, ct).ConfigureAwait(false);
                if (rate is not null)
                {
                    rateResults.Add(rate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cargo provider {Provider} rate query failed", adapter.Provider);
            }
        }

        if (rateResults.Count == 0)
        {
            _logger.LogInformation(
                "No ICargoRateProvider returned rates for order {OrderId}, falling back to AvailabilityFirst",
                order.Id);
            return await SelectByAvailabilityAsync(order, ct).ConfigureAwait(false);
        }

        var best = sortByPrice
            ? rateResults.OrderBy(r => r.Price).First()
            : rateResults.OrderBy(r => r.EstimatedDelivery).First();

        var strategyName = sortByPrice ? "CheapestFirst" : "FastestFirst";
        _logger.LogInformation(
            "Selected cargo provider {Provider} for order {OrderId} ({Strategy}) — Price={Price} {Currency}, ETA={ETA}",
            best.Provider, order.Id, strategyName, best.Price, best.Currency, best.EstimatedDelivery);

        return best.Provider;
    }

    /// <summary>
    /// Siparis bilgilerinden varsayilan ShipmentRequest olusturur.
    /// </summary>
    private static ShipmentRequest BuildDefaultShipmentRequest(Order order)
    {
        return new ShipmentRequest
        {
            OrderId = order.Id,
            RecipientName = order.CustomerName ?? string.Empty,
            Weight = 1, // Varsayilan agirlik
            Desi = 1,   // Varsayilan desi
            ParcelCount = 1
        };
    }
}
