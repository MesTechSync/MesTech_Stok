using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA Bot'un bildirim gonderim sonucunu consume eder.
/// Basarili ise NotificationLog'u Sent, basarisiz ise Failed olarak kaydeder.
/// </summary>
public class NotificationSentConsumer : IConsumer<BotNotificationSentEvent>
{
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ILogger<NotificationSentConsumer> _logger;

    public NotificationSentConsumer(
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ILogger<NotificationSentConsumer> logger)
    {
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _monitor = monitor;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BotNotificationSentEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "[MESA Consumer] Bildirim sonucu alindi: Channel={Channel}, Recipient={Recipient}, Success={Success}",
            msg.Channel, msg.Recipient, msg.Success);

        var channel = ParseChannel(msg.Channel);

        var log = NotificationLog.Create(
            msg.TenantId,
            channel,
            msg.Recipient,
            msg.TemplateName,
            msg.Content);

        if (msg.Success)
        {
            log.MarkAsSent();
            _logger.LogInformation(
                "[MESA Consumer] Bildirim basarili: Channel={Channel}, Recipient={Recipient}, Template={TemplateName}",
                msg.Channel, msg.Recipient, msg.TemplateName);
        }
        else
        {
            var errorMessage = msg.ErrorMessage ?? "Unknown error";
            log.MarkAsFailed(errorMessage);
            _logger.LogWarning(
                "[MESA Consumer] Bildirim basarisiz: Channel={Channel}, Recipient={Recipient}, Error={ErrorMessage}",
                msg.Channel, msg.Recipient, errorMessage);
        }

        await _notificationLogRepository.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _monitor.RecordConsume("bot.notification.sent");

        _logger.LogInformation(
            "[MESA Consumer] NotificationLog kaydedildi: Id={LogId}, Status={Status}",
            log.Id, log.Status);
    }

    /// <summary>
    /// Kanal string degerini NotificationChannel enum'a donusturur.
    /// Bilinmeyen degerler icin varsayilan Email doner.
    /// </summary>
    private DomainChannel ParseChannel(string channel)
    {
        return channel.ToLowerInvariant() switch
        {
            "whatsapp" => DomainChannel.WhatsApp,
            "telegram" => DomainChannel.Telegram,
            "email" => DomainChannel.Email,
            "push" => DomainChannel.Push,
            "sms" => DomainChannel.SMS,
            _ => HandleUnknownChannel(channel)
        };
    }

    private DomainChannel HandleUnknownChannel(string channel)
    {
        _logger.LogWarning(
            "[MESA Consumer] Bilinmeyen bildirim kanali: {Channel}, varsayilan Email kullaniliyor",
            channel);
        return DomainChannel.Email;
    }
}
