using MesTech.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoProviders;

public sealed class GetCargoProvidersHandler : IRequestHandler<GetCargoProvidersQuery, IReadOnlyList<CargoProviderDto>>
{
    private readonly ICargoProviderFactory _cargoProviderFactory;
    private readonly ILogger<GetCargoProvidersHandler> _logger;

    public GetCargoProvidersHandler(
        ICargoProviderFactory cargoProviderFactory,
        ILogger<GetCargoProvidersHandler> logger)
    {
        _cargoProviderFactory = cargoProviderFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CargoProviderDto>> Handle(
        GetCargoProvidersQuery request, CancellationToken cancellationToken)
    {
        var adapters = _cargoProviderFactory.GetAll();
        var results = new List<CargoProviderDto>(adapters.Count);

        foreach (var adapter in adapters)
        {
            var isAvailable = false;
            try
            {
                isAvailable = await adapter.IsAvailableAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cargo provider {Provider} availability check failed", adapter.Provider);
            }

            results.Add(new CargoProviderDto
            {
                Name = adapter.Provider.ToString(),
                Code = adapter.Provider.ToString(),
                IsActive = isAvailable,
                ContractInfo = BuildContractInfo(adapter),
                AvgDeliveryDays = EstimateDeliveryDays(adapter)
            });
        }

        return results.AsReadOnly();
    }

    private static string BuildContractInfo(ICargoAdapter adapter)
    {
        var features = new List<string>(4);
        if (adapter.SupportsCashOnDelivery) features.Add("COD");
        if (adapter.SupportsMultiParcel) features.Add("MultiParcel");
        if (adapter.SupportsLabelGeneration) features.Add("Label");
        if (adapter.SupportsCancellation) features.Add("Cancel");
        return string.Join(", ", features);
    }

    private static int EstimateDeliveryDays(ICargoAdapter adapter) =>
        adapter.Provider switch
        {
            Domain.Enums.CargoProvider.Hepsijet => 1,
            Domain.Enums.CargoProvider.YurticiKargo => 2,
            Domain.Enums.CargoProvider.ArasKargo => 2,
            Domain.Enums.CargoProvider.SuratKargo => 2,
            Domain.Enums.CargoProvider.MngKargo => 3,
            Domain.Enums.CargoProvider.PttKargo => 4,
            Domain.Enums.CargoProvider.UPS => 3,
            Domain.Enums.CargoProvider.DHL => 3,
            Domain.Enums.CargoProvider.FedEx => 3,
            Domain.Enums.CargoProvider.Sendeo => 3,
            _ => 5
        };
}
