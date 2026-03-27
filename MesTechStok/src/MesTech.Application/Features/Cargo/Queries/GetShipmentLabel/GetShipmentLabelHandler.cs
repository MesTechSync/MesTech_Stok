using MediatR;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;

public sealed class GetShipmentLabelHandler : IRequestHandler<GetShipmentLabelQuery, ShipmentLabelResult>
{
    private readonly IEnumerable<ICargoAdapter> _cargoAdapters;
    private readonly ILogger<GetShipmentLabelHandler> _logger;

    public GetShipmentLabelHandler(IEnumerable<ICargoAdapter> cargoAdapters, ILogger<GetShipmentLabelHandler> logger)
    {
        _cargoAdapters = cargoAdapters;
        _logger = logger;
    }

    public async Task<ShipmentLabelResult> Handle(GetShipmentLabelQuery request, CancellationToken cancellationToken)
    {
        foreach (var adapter in _cargoAdapters.Where(a => a.SupportsLabelGeneration))
        {
            try
            {
                var result = await adapter.GetShipmentLabelAsync(request.ShipmentId, cancellationToken).ConfigureAwait(false);
                return new ShipmentLabelResult
                {
                    IsSuccess = true,
                    LabelData = result.Data,
                    ContentType = "application/pdf",
                    FileName = result.FileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Label generation failed for adapter {Provider}", adapter.Provider);
            }
        }

        return new ShipmentLabelResult { ErrorMessage = "Etiket uretilemedi" };
    }
}
