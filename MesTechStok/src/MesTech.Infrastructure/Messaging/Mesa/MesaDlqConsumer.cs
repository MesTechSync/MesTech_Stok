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

        try
        {
            _logger.LogError(
                "[MESA DLQ] Mesaj basarisiz: MessageId={MessageId}, type={MessageType}, hata={Error}, timestamp={Timestamp}",
                context.MessageId, messageType, exceptionMessage, fault.Timestamp);

            _logger.LogWarning(
                "DLQ message received — MessageId={MessageId}, Source: {Source}, Error: {Error}, Payload: {Payload}",
                context.MessageId, messageType, exceptionMessage,
                System.Text.Json.JsonSerializer.Serialize(context.Message));

            _monitor.RecordError(messageType, exceptionMessage);
        }
        catch (Exception ex)
        {
            // DLQ consumer should never throw — swallow to prevent infinite retry loop
            _logger.LogError(ex, "[MESA DLQ] DLQ consumer itself failed for MessageId={MessageId}",
                context.MessageId);
        }

        return Task.CompletedTask;
    }
}
