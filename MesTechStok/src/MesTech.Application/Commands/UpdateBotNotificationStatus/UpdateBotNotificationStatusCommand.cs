using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.UpdateBotNotificationStatus;

public record UpdateBotNotificationStatusCommand : IRequest
{
    public string Channel { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class UpdateBotNotificationStatusHandler : IRequestHandler<UpdateBotNotificationStatusCommand>
{
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateBotNotificationStatusHandler> _logger;

    public UpdateBotNotificationStatusHandler(
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateBotNotificationStatusHandler> logger)
    {
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UpdateBotNotificationStatusCommand request, CancellationToken cancellationToken)
    {
        var notification = NotificationLog.Create(
            request.TenantId,
            MesTech.Domain.Enums.NotificationChannel.Push,
            request.Recipient.Length > 0 ? request.Recipient : "unknown",
            $"Bot Notification: {request.Channel}",
            request.Success
                ? $"Bildirim basarili: {request.Channel}"
                : $"Bildirim hatasi: {request.ErrorMessage}");

        if (request.Success)
            notification.MarkAsSent();
        else
            notification.MarkAsFailed(request.ErrorMessage ?? "Unknown error");

        await _notificationLogRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "UpdateBotNotificationStatus: NotificationLog saved — Channel={Channel}, Success={Success}",
            request.Channel, request.Success);
    }
}
