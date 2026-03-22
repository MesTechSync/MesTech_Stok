using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface INotificationSettingsUpdatedEventHandler
{
    Task HandleAsync(Guid userId, NotificationChannel channel, bool isEnabled, CancellationToken ct);
}

public class NotificationSettingsUpdatedEventHandler : INotificationSettingsUpdatedEventHandler
{
    private readonly ILogger<NotificationSettingsUpdatedEventHandler> _logger;

    public NotificationSettingsUpdatedEventHandler(ILogger<NotificationSettingsUpdatedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid userId, NotificationChannel channel, bool isEnabled, CancellationToken ct)
    {
        _logger.LogInformation(
            "Bildirim ayarları güncellendi — UserId={UserId}, Channel={Channel}, Enabled={IsEnabled}",
            userId, channel, isEnabled);

        return Task.CompletedTask;
    }
}
