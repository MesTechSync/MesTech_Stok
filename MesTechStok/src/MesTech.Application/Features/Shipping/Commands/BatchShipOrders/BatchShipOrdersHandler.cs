using MediatR;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Shipping.Commands.BatchShipOrders;

public class BatchShipOrdersHandler : IRequestHandler<BatchShipOrdersCommand, BatchShipResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BatchShipOrdersHandler> _logger;

    public BatchShipOrdersHandler(IMediator mediator, ILogger<BatchShipOrdersHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BatchShipResult> Handle(BatchShipOrdersCommand request, CancellationToken cancellationToken)
    {
        var results = new List<AutoShipResult>();

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                var command = new AutoShipOrderCommand(request.TenantId, orderId);
                var result = await _mediator.Send(command, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-ship order {OrderId}", orderId);
                results.Add(AutoShipResult.Failed($"Unexpected error for order {orderId}: {ex.Message}"));
            }
        }

        return BatchShipResult.Create(results);
    }
}
