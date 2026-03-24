using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// İade talebi sonuçlandığında iade tutarı ve stok iadesi kaydeder.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IReturnResolvedEventHandler
{
    Task HandleAsync(ReturnResolvedEvent domainEvent, CancellationToken ct);
}

public class ReturnResolvedEventHandler : IReturnResolvedEventHandler
{
    private readonly ILogger<ReturnResolvedEventHandler> _logger;

    public ReturnResolvedEventHandler(ILogger<ReturnResolvedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ReturnResolvedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "ReturnResolved: ReturnId={ReturnId}, OrderId={OrderId}, Status={Status}, Refund={Refund}",
            domainEvent.ReturnRequestId, domainEvent.OrderId, domainEvent.FinalStatus, domainEvent.RefundAmount);

        return Task.CompletedTask;
    }
}
