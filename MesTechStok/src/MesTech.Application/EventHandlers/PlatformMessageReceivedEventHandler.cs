using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IPlatformMessageReceivedEventHandler
{
    Task HandleAsync(Guid messageId, PlatformType platform, string senderName, CancellationToken ct);
}

public class PlatformMessageReceivedEventHandler : IPlatformMessageReceivedEventHandler
{
    private readonly ILogger<PlatformMessageReceivedEventHandler> _logger;

    public PlatformMessageReceivedEventHandler(ILogger<PlatformMessageReceivedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid messageId, PlatformType platform, string senderName, CancellationToken ct)
    {
        _logger.LogInformation(
            "Platform mesajı alındı — MessageId={MessageId}, Platform={Platform}, Sender={Sender}",
            messageId, platform, senderName);

        return Task.CompletedTask;
    }
}
