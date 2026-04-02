using MediatR;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Shipping.Commands.BatchShipOrders;

public sealed class BatchShipOrdersHandler : IRequestHandler<BatchShipOrdersCommand, BatchShipResult>
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
        ArgumentNullException.ThrowIfNull(request);
        var results = new List<AutoShipResult>();

        foreach (var orderId in request.OrderIds)
        {
#pragma warning disable CA1031 // Intentional: each order failure must not stop the batch — failures are collected
            try
            {
                var command = new AutoShipOrderCommand(request.TenantId, orderId);
                var result = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-ship order {OrderId}", orderId);
                results.Add(AutoShipResult.Failed($"Unexpected error for order {orderId}: {ex.Message}"));
            }
#pragma warning restore CA1031
        }

        return BatchShipResult.Create(results);
    }
}
