using MassTransit;
using MediatR;
using MesTech.Application.Commands.GenerateEFatura;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA Bot e-fatura talep etti — WhatsApp/Telegram kanali mekanizmasi.
/// Talebi NotificationLog olarak kaydeder, muhasebe ekibini bilgilendirir.
/// </summary>
public sealed class BotEFaturaRequestedConsumer : IConsumer<BotEFaturaRequestedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<BotEFaturaRequestedConsumer> _logger;

    public BotEFaturaRequestedConsumer(
        IMediator mediator,
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<BotEFaturaRequestedConsumer> logger)
    {
        _mediator = mediator;
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BotEFaturaRequestedIntegrationEvent> context)
    {
        var msg = context.Message;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning(
                "[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(BotEFaturaRequestedIntegrationEvent), context.MessageId);

        try
        {
            await _mediator.Send(new GenerateEFaturaCommand
            {
                BotUserId = msg.BotUserId,
                OrderId = msg.OrderId,
                BuyerVkn = msg.BuyerVkn,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(BotEFaturaRequestedIntegrationEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] Bot e-fatura talebi geldi: BotUserId={BotUserId}, " +
            "OrderId={OrderId}, BuyerVkn={BuyerVkn}, TenantId={TenantId}",
            msg.BotUserId, msg.OrderId, msg.BuyerVkn, tenantId);

        try
        {
            // Record the bot e-invoice request as a notification for accounting team
            var orderInfo = msg.OrderId.HasValue ? $"OrderId: {msg.OrderId}" : "OrderId: -";
            var vknInfo = !string.IsNullOrWhiteSpace(msg.BuyerVkn) ? $"VKN: {msg.BuyerVkn}" : "VKN: -";

            var notification = NotificationLog.Create(
                tenantId,
                DomainChannel.Push,
                "accounting",
                "Bot E-Fatura Request",
                $"Bot e-fatura talebi — BotUser: {msg.BotUserId}, {orderInfo}, {vknInfo}. " +
                $"Muhasebe islemi bekliyor.");
            notification.MarkAsSent();

            await _notificationLogRepository.AddAsync(notification).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "[MESA Consumer] Bot e-fatura talebi NotificationLog olarak kaydedildi: " +
                "NotificationId={NotificationId}, BotUserId={BotUserId}",
                notification.Id, msg.BotUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MESA Consumer] Bot e-fatura talebi islenirken hata: BotUserId={BotUserId}",
                msg.BotUserId);
            throw; // MassTransit retry policy
        }

        _monitor.RecordConsume("bot.efatura.requested");
    }
}
