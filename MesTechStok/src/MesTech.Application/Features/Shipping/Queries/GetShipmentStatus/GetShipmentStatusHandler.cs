using MediatR;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;

public sealed class GetShipmentStatusHandler : IRequestHandler<GetShipmentStatusQuery, ShipmentStatusDto>
{
    private readonly ICargoProviderFactory _cargoProviderFactory;

    public GetShipmentStatusHandler(ICargoProviderFactory cargoProviderFactory)
        => _cargoProviderFactory = cargoProviderFactory;

    public async Task<ShipmentStatusDto> Handle(GetShipmentStatusQuery request, CancellationToken cancellationToken)
    {
        var adapter = _cargoProviderFactory.Resolve(request.Provider)
            ?? throw new InvalidOperationException(
                $"No cargo adapter registered for provider {request.Provider}.");

        var trackingResult = await adapter.TrackShipmentAsync(request.TrackingNumber, cancellationToken);

        return new ShipmentStatusDto
        {
            TrackingNumber = trackingResult.TrackingNumber,
            Provider = request.Provider,
            Status = trackingResult.Status,
            EstimatedDelivery = trackingResult.EstimatedDelivery,
            Events = trackingResult.Events.Select(e => new ShipmentEventDto
            {
                Timestamp = e.Timestamp,
                Location = e.Location,
                Description = e.Description,
                Status = e.Status
            }).ToList()
        };
    }
}
