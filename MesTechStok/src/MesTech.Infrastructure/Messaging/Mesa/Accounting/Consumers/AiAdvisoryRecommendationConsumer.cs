using MassTransit;
using MediatR;
using MesTech.Application.Commands.ProcessAiRecommendation;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI finansal danismanlik onerilerini consume eder.
/// Onerileri NotificationLog olarak kaydeder ve dashboard'a bildirir.
/// </summary>
public sealed class AiAdvisoryRecommendationConsumer : IConsumer<AiAdvisoryRecommendationEvent>
{
    private readonly IMediator _mediator;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AiAdvisoryRecommendationConsumer> _logger;

    public AiAdvisoryRecommendationConsumer(
        IMediator mediator,
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AiAdvisoryRecommendationConsumer> logger)
    {
        _mediator = mediator;
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiAdvisoryRecommendationEvent> context)
    {
        var msg = context.Message;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning(
                "[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogError(
                "[MESA Consumer] TenantId is Guid.Empty after fallback — aborting. MessageId={MessageId}",
                context.MessageId);
            _monitor.RecordError("ai.advisory.recommendation", "TenantId is Guid.Empty — aborted");
            throw new InvalidOperationException("TenantId is Guid.Empty — message rejected to prevent cross-tenant data leak");
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(AiAdvisoryRecommendationEvent), context.MessageId);

        try
        {
            await _mediator.Send(new ProcessAiRecommendationCommand
            {
                RecommendationType = msg.RecommendationType,
                Title = msg.Title,
                Description = msg.Description,
                ActionUrl = msg.ActionUrl,
                Priority = msg.Priority,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(AiAdvisoryRecommendationEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI danismanlik onerisi alindi: " +
            "Tip={RecommendationType}, Baslik={Title}, Oncelik={Priority}",
            msg.RecommendationType, msg.Title, msg.Priority);

        _logger.LogInformation(
            "[MESA Consumer] Oneri detayi: {Description}",
            msg.Description);

        if (!string.IsNullOrWhiteSpace(msg.ActionUrl))
        {
            _logger.LogInformation(
                "[MESA Consumer] Aksiyon URL: {ActionUrl}", msg.ActionUrl);
        }

        try
        {
            // Create notification log for the advisory recommendation
            var notification = NotificationLog.Create(
                tenantId,
                DomainChannel.Push,
                "system",
                $"AI Advisory: {msg.RecommendationType}",
                $"{msg.Title}: {msg.Description}");
            notification.MarkAsSent();

            await _notificationLogRepository.AddAsync(notification, context.CancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "[MESA Consumer] AI onerisi NotificationLog olarak kaydedildi: NotificationId={NotificationId}",
                notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] AI danismanlik onerisi islenirken hata");
            throw; // MassTransit retry policy
        }

        // Dashboard bildirim sistemi icin kaydet (IDashboardNotifier WebSocket push eklenecek)
        _logger.LogInformation(
            "[MESA Consumer] AI onerisi dashboard bildirimine kaydedildi: " +
            "Tip={RecommendationType}, Oncelik={Priority}",
            msg.RecommendationType, msg.Priority);

        _monitor.RecordConsume("ai.advisory.recommendation");
    }
}
