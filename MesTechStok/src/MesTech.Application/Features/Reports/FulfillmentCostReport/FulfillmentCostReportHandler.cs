using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Reports.FulfillmentCostReport;

public sealed class FulfillmentCostReportHandler
    : IRequestHandler<FulfillmentCostReportQuery, FulfillmentCostReportDto>
{
    private readonly IFulfillmentProviderFactory _factory;
    private readonly IShipmentCostRepository _shipmentCostRepo;
    private readonly ILogger<FulfillmentCostReportHandler> _logger;

    public FulfillmentCostReportHandler(
        IFulfillmentProviderFactory factory,
        IShipmentCostRepository shipmentCostRepo,
        ILogger<FulfillmentCostReportHandler> logger)
    {
        _factory = factory;
        _shipmentCostRepo = shipmentCostRepo;
        _logger = logger;
    }

    public async Task<FulfillmentCostReportDto> Handle(
        FulfillmentCostReportQuery request, CancellationToken cancellationToken)
    {
        var centers = request.CenterFilter.HasValue
            ? new[] { request.CenterFilter.Value }
            : Enum.GetValues<FulfillmentCenter>();

        var centerCosts = new List<CenterCostDto>();
        decimal totalCost = 0;

        foreach (var center in centers)
        {
            var provider = _factory.Resolve(center);
            if (provider is null) continue;

            // Availability probe: any failure means center is unavailable for reporting
#pragma warning disable CA1031 // Intentional broad catch — availability probe treats all failures as "unavailable"
            bool isAvailable;
            try
            {
                isAvailable = await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fulfillment center {Center} availability check failed", center);
                isAvailable = false;
            }
#pragma warning restore CA1031

            if (!isAvailable)
            {
                centerCosts.Add(new CenterCostDto(center, center.ToString(), 0, 0, 0, 0, false));
                continue;
            }

            // Cost query can fail due to network/auth/data issues — report degrades gracefully per center
#pragma warning disable CA1031 // Intentional broad catch — report returns zero-cost fallback on query failure
            try
            {
                var orders = await provider.GetFulfillmentOrdersAsync(request.StartDate, cancellationToken).ConfigureAwait(false);
                var periodOrders = orders
                    .Where(o => o.ShippedDate >= request.StartDate && o.ShippedDate <= request.EndDate)
                    .ToList();

                var totalItems = periodOrders.Sum(o => o.Items.Sum(i => i.QuantityShipped));
                var totalOrderCount = periodOrders.Count;

                // Use real shipment cost data from DB when available, fallback to estimate
                var shipmentCosts = await _shipmentCostRepo.GetByDateRangeAsync(
                    request.TenantId, request.StartDate, request.EndDate, cancellationToken).ConfigureAwait(false);
                var realCost = shipmentCosts.Sum(sc => sc.Cost);
                var estimatedFee = realCost > 0 ? realCost : totalItems * 15m; // fallback estimate if no cost records
                var inventoryValue = 0m;

                centerCosts.Add(new CenterCostDto(
                    center, center.ToString(), totalItems, inventoryValue,
                    estimatedFee, totalItems > 0 ? Math.Round(estimatedFee / totalItems, 2) : 0, true));

                totalCost += estimatedFee;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fulfillment cost query failed for {Center}", center);
                centerCosts.Add(new CenterCostDto(center, center.ToString(), 0, 0, 0, 0, false));
            }
#pragma warning restore CA1031
        }

        return new FulfillmentCostReportDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalFulfillmentCost = totalCost,
            Centers = centerCosts
        };
    }
}
