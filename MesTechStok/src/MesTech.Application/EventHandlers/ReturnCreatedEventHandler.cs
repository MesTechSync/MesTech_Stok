using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// İade talebi oluşturulduğunda bildirim ve istatistik günceller.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IReturnCreatedEventHandler
{
    Task HandleAsync(ReturnCreatedEvent domainEvent, CancellationToken ct);
}

public sealed class ReturnCreatedEventHandler : IReturnCreatedEventHandler
{
    private readonly ILogger<ReturnCreatedEventHandler> _logger;

    public ReturnCreatedEventHandler(ILogger<ReturnCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ReturnCreatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "ReturnCreated: ReturnId={ReturnId}, OrderId={OrderId}, Platform={Platform}, Reason={Reason}",
            domainEvent.ReturnRequestId, domainEvent.OrderId, domainEvent.Platform, domainEvent.Reason);

        return Task.CompletedTask;
    }
}
