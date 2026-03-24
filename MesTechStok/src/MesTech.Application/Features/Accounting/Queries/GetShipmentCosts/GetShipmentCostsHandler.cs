using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;

public class GetShipmentCostsHandler
    : IRequestHandler<GetShipmentCostsQuery, IReadOnlyList<ShipmentCostDto>>
{
    private readonly IShipmentCostRepository _repo;
    private readonly ILogger<GetShipmentCostsHandler> _logger;

    public GetShipmentCostsHandler(
        IShipmentCostRepository repo,
        ILogger<GetShipmentCostsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ShipmentCostDto>> Handle(
        GetShipmentCostsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.UtcNow.AddMonths(-1);
        var to = request.To ?? DateTime.UtcNow;

        var costs = await _repo.GetByDateRangeAsync(
            request.TenantId, from, to, cancellationToken).ConfigureAwait(false);

        return costs.Select(c => new ShipmentCostDto(
            c.Id, c.OrderId, c.Provider, c.Cost, c.NetCost,
            c.TrackingNumber, c.ShippedAt, c.IsChargedToCustomer)).ToList();
    }
}
