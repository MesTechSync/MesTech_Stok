using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// Dead Letter Queue consumer — MassTransit _error queue'dan gelen
/// basarisiz mesajlari loglar ve izler.
/// </summary>
public sealed class MesaDlqConsumer : IConsumer<Fault>
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
        _logger.LogInformation("Processing {Consumer} MessageId={MessageId}",
            nameof(MesaDlqConsumer), context.MessageId);

        try
        {
            var fault = context.Message;
            var messageType = fault.FaultMessageTypes?.FirstOrDefault() ?? "unknown";
            var exceptionMessage = fault.Exceptions?.FirstOrDefault()?.Message ?? "No exception details";

            _logger.LogError(
                "[MESA DLQ] Mesaj basarisiz: type={MessageType}, hata={Error}, timestamp={Timestamp}, MessageId={MessageId}",
                messageType, exceptionMessage, fault.Timestamp, context.MessageId);

            _monitor.RecordError(messageType, exceptionMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "DLQ Consumer {Consumer} failed for MessageId={MessageId}",
                nameof(MesaDlqConsumer), context.MessageId);
        }

        return Task.CompletedTask;
    }
}
