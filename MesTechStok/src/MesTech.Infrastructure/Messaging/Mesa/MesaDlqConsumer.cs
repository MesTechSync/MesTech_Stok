using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// Dead Letter Queue consumer — MassTransit _error queue'dan gelen
/// basarisiz mesajlari loglar ve izler.
/// </summary>
public class MesaDlqConsumer : IConsumer<Fault>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<MesaDlqConsumer> _logger;

    public MesaDlqConsumer(
        IMesaEventMonitor monitor,
        ILogger<MesaDlqConsumer> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault> context)
    {
        var fault = context.Message;
        var messageType = fault.FaultMessageTypes?.FirstOrDefault() ?? "unknown";
        var exceptionMessage = fault.Exceptions?.FirstOrDefault()?.Message ?? "No exception details";

        _logger.LogError(
            "[MESA DLQ] Mesaj basarisiz: type={MessageType}, hata={Error}, timestamp={Timestamp}",
            messageType, exceptionMessage, fault.Timestamp);

        _monitor.RecordError(messageType, exceptionMessage);

        return Task.CompletedTask;
    }
}
