using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Orders.Queries.GetStaleOrders;

public class GetStaleOrdersQueryHandler
    : IRequestHandler<GetStaleOrdersQuery, IReadOnlyList<StaleOrderDto>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ILogger<GetStaleOrdersQueryHandler> _logger;

    public GetStaleOrdersQueryHandler(
        IOrderRepository orderRepo,
        ILogger<GetStaleOrdersQueryHandler> logger)
    {
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StaleOrderDto>> Handle(
        GetStaleOrdersQuery request, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - request.EffectiveThreshold;

        var staleOrders = await _orderRepo.GetStaleOrdersAsync(
            request.TenantId, cutoff, ct).ConfigureAwait(false);

        var result = staleOrders.Select(o => new StaleOrderDto(
            o.Id,
            o.OrderNumber,
            o.SourcePlatform,
            o.OrderDate,
            DateTime.UtcNow - o.OrderDate,
            o.CustomerName)).ToList();

        if (result.Count > 0)
        {
            _logger.LogWarning(
                "Stale orders detected: {Count} orders older than {Threshold}h — TenantId={TenantId}",
                result.Count, request.EffectiveThreshold.TotalHours, request.TenantId);
        }

        return result;
    }
}
