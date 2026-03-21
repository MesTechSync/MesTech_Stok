using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Queries.GetCargoComparison;

/// <summary>
/// Kargo karsilastirma sorgusu isleyicisi.
/// Tum kayitli kargo adaptorlerinden fiyat sorgusu yapar ve karsilastirir.
/// </summary>
public class GetCargoComparisonHandler
    : IRequestHandler<GetCargoComparisonQuery, CargoComparisonResult>
{
    private readonly ICargoProviderFactory _factory;
    private readonly ILogger<GetCargoComparisonHandler> _logger;

    public GetCargoComparisonHandler(
        ICargoProviderFactory factory,
        ILogger<GetCargoComparisonHandler> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<CargoComparisonResult> Handle(
        GetCargoComparisonQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var allAdapters = _factory.GetAll();
        var items = new List<CargoComparisonItem>();

        foreach (var adapter in allAdapters)
        {
            var item = await BuildComparisonItemAsync(adapter, request, cancellationToken);
            items.Add(item);
        }

        var availableWithRates = items.Where(i => i.IsAvailable && i.ErrorMessage is null).ToList();

        return new CargoComparisonResult
        {
            Items = items.AsReadOnly(),
            CheapestProvider = availableWithRates.Count > 0
                ? availableWithRates.OrderBy(i => i.Price).First().Provider
                : null,
            FastestProvider = availableWithRates.Count > 0
                ? availableWithRates.OrderBy(i => i.EstimatedDelivery).First().Provider
                : null
        };
    }

    private async Task<CargoComparisonItem> BuildComparisonItemAsync(
        ICargoAdapter adapter,
        GetCargoComparisonQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isAvailable = await adapter.IsAvailableAsync(cancellationToken).ConfigureAwait(false);

            if (!isAvailable)
            {
                return new CargoComparisonItem
                {
                    Provider = adapter.Provider,
                    IsAvailable = false,
                    ErrorMessage = "Provider is not available"
                };
            }

            if (adapter is ICargoRateProvider rateProvider)
                return await GetRateItemAsync(adapter, rateProvider, request, cancellationToken);

            return new CargoComparisonItem
            {
                Provider = adapter.Provider,
                IsAvailable = true,
                ErrorMessage = "Provider does not support rate queries"
            };
        }
#pragma warning disable CA1031 // Intentional: each provider failure must not stop comparison of others
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cargo comparison failed for provider {Provider}", adapter.Provider);
            return new CargoComparisonItem
            {
                Provider = adapter.Provider,
                IsAvailable = false,
                ErrorMessage = ex.Message
            };
        }
#pragma warning restore CA1031
    }

    private static async Task<CargoComparisonItem> GetRateItemAsync(
        ICargoAdapter adapter,
        ICargoRateProvider rateProvider,
        GetCargoComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var rate = await rateProvider.GetRateAsync(request.ShipmentRequest, cancellationToken)
            .ConfigureAwait(false);

        if (rate is not null)
        {
            return new CargoComparisonItem
            {
                Provider = rate.Provider,
                Price = rate.Price,
                Currency = rate.Currency,
                EstimatedDelivery = rate.EstimatedDelivery,
                IncludesVat = rate.IncludesVat,
                IsAvailable = true
            };
        }

        return new CargoComparisonItem
        {
            Provider = adapter.Provider,
            IsAvailable = true,
            ErrorMessage = "Rate query returned null"
        };
    }
}
