using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Application.Features.Notifications.Commands.SendNotification;

/// <summary>
/// Bildirim gonderme handler'i.
/// NotificationLog olusturur (Pending), IMessagePublisher uzerinden MESA Bot'a publish eder.
/// MESA Bot baglantisi yoksa Pending olarak kaydeder — consumer sonradan isler.
/// </summary>
public class SendNotificationHandler : IRequestHandler<SendNotificationCommand, Guid>
{
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<SendNotificationHandler> _logger;

    public SendNotificationHandler(
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        IMesaEventMonitor monitor,
        ILogger<SendNotificationHandler> logger)
    {
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _monitor = monitor;
        _logger = logger;
    }

    public async Task<Guid> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var channel = ParseChannel(request.Channel);

        var log = NotificationLog.Create(
            request.TenantId,
            channel,
            request.Recipient,
            request.TemplateName,
            request.Content);

        await _notificationLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Notification] NotificationLog olusturuldu: Id={LogId}, Channel={Channel}, Recipient={Recipient}",
            log.Id, request.Channel, request.Recipient);

        try
        {
            await _messagePublisher.PublishAsync(new SendNotificationMessage
            {
                NotificationLogId = log.Id,
                TenantId = request.TenantId,
                Channel = request.Channel,
                Recipient = request.Recipient,
                TemplateName = request.TemplateName,
                Content = request.Content,
                RequestedAt = DateTime.UtcNow
            }, cancellationToken);

            _monitor.RecordPublish("notification.send.requested");

            _logger.LogInformation(
                "[Notification] MESA Bot'a bildirim istegi yayimlandi: LogId={LogId}, Channel={Channel}",
                log.Id, request.Channel);
        }
#pragma warning disable CA1031 // Intentional: publish failure must not prevent returning the persisted log ID
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Notification] MESA Bot'a publish basarisiz — bildirim Pending olarak kaldi: LogId={LogId}",
                log.Id);
        }
#pragma warning restore CA1031

        return log.Id;
    }

    private static DomainChannel ParseChannel(string channel)
    {
        return channel.ToUpperInvariant() switch
        {
            "WHATSAPP" => DomainChannel.WhatsApp,
            "TELEGRAM" => DomainChannel.Telegram,
            "EMAIL" => DomainChannel.Email,
            "PUSH" => DomainChannel.Push,
            "SMS" => DomainChannel.SMS,
            _ => DomainChannel.Email
        };
    }
}
