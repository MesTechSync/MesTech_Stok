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
        var allAdapters = _factory.GetAll();
        var items = new List<CargoComparisonItem>();

        foreach (var adapter in allAdapters)
        {
            try
            {
                var isAvailable = await adapter.IsAvailableAsync(cancellationToken).ConfigureAwait(false);

                if (!isAvailable)
                {
                    items.Add(new CargoComparisonItem
                    {
                        Provider = adapter.Provider,
                        IsAvailable = false,
                        ErrorMessage = "Provider is not available"
                    });
                    continue;
                }

                if (adapter is ICargoRateProvider rateProvider)
                {
                    var rate = await rateProvider.GetRateAsync(request.ShipmentRequest, cancellationToken)
                        .ConfigureAwait(false);

                    if (rate is not null)
                    {
                        items.Add(new CargoComparisonItem
                        {
                            Provider = rate.Provider,
                            Price = rate.Price,
                            Currency = rate.Currency,
                            EstimatedDelivery = rate.EstimatedDelivery,
                            IncludesVat = rate.IncludesVat,
                            IsAvailable = true
                        });
                    }
                    else
                    {
                        items.Add(new CargoComparisonItem
                        {
                            Provider = adapter.Provider,
                            IsAvailable = true,
                            ErrorMessage = "Rate query returned null"
                        });
                    }
                }
                else
                {
                    items.Add(new CargoComparisonItem
                    {
                        Provider = adapter.Provider,
                        IsAvailable = true,
                        ErrorMessage = "Provider does not support rate queries"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cargo comparison failed for provider {Provider}", adapter.Provider);
                items.Add(new CargoComparisonItem
                {
                    Provider = adapter.Provider,
                    IsAvailable = false,
                    ErrorMessage = ex.Message
                });
            }
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
}
