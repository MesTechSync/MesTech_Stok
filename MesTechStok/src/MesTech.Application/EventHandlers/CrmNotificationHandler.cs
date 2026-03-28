using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// CRM yaşam döngüsü bildirimleri — Deal/Lead/Calendar event'leri.
/// DealWonEvent, DealLostEvent, DealStageChangedEvent,
/// LeadConvertedEvent, LeadScoredEvent, CalendarEventCreatedEvent → NotificationLog.
/// </summary>
public interface ICrmNotificationHandler
{
    Task HandleDealWonAsync(Guid dealId, Guid tenantId, CancellationToken ct);
    Task HandleDealLostAsync(Guid dealId, Guid tenantId, string? reason, CancellationToken ct);
    Task HandleDealStageChangedAsync(Guid dealId, Guid tenantId, string newStage, CancellationToken ct);
    Task HandleLeadConvertedAsync(Guid leadId, Guid tenantId, CancellationToken ct);
    Task HandleLeadScoredAsync(Guid leadId, Guid tenantId, int score, CancellationToken ct);
    Task HandleCalendarEventCreatedAsync(Guid eventId, Guid tenantId, string title, CancellationToken ct);
}

public sealed class CrmNotificationHandler : ICrmNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CrmNotificationHandler> _logger;

    public CrmNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<CrmNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleDealWonAsync(Guid dealId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation("DealWon → bildirim. DealId={Id}", dealId);
        await CreateNotificationAsync(tenantId, "DealWon", $"Anlaşma kazanıldı — ID: {dealId}", ct);
    }

    public async Task HandleDealLostAsync(Guid dealId, Guid tenantId, string? reason, CancellationToken ct)
    {
        _logger.LogWarning("DealLost → bildirim. DealId={Id}, Reason={Reason}", dealId, reason);
        await CreateNotificationAsync(tenantId, "DealLost",
            $"Anlaşma kaybedildi — ID: {dealId}, Sebep: {reason ?? "belirtilmedi"}", ct);
    }

    public async Task HandleDealStageChangedAsync(Guid dealId, Guid tenantId, string newStage, CancellationToken ct)
    {
        _logger.LogInformation("DealStageChanged → bildirim. DealId={Id}, Stage={Stage}", dealId, newStage);
        await CreateNotificationAsync(tenantId, "DealStageChanged",
            $"Anlaşma aşaması değişti — ID: {dealId}, Yeni Aşama: {newStage}", ct);
    }

    public async Task HandleLeadConvertedAsync(Guid leadId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation("LeadConverted → bildirim. LeadId={Id}", leadId);
        await CreateNotificationAsync(tenantId, "LeadConverted", $"Lead dönüştürüldü — ID: {leadId}", ct);
    }

    public async Task HandleLeadScoredAsync(Guid leadId, Guid tenantId, int score, CancellationToken ct)
    {
        _logger.LogInformation("LeadScored → bildirim. LeadId={Id}, Score={Score}", leadId, score);
        await CreateNotificationAsync(tenantId, "LeadScored",
            $"Lead puanlandı — ID: {leadId}, Puan: {score}", ct);
    }

    public async Task HandleCalendarEventCreatedAsync(Guid eventId, Guid tenantId, string title, CancellationToken ct)
    {
        _logger.LogInformation("CalendarEventCreated → bildirim. EventId={Id}, Title={Title}", eventId, title);
        await CreateNotificationAsync(tenantId, "CalendarEventCreated",
            $"Takvim etkinliği oluşturuldu — {title}", ct);
    }

    private async Task CreateNotificationAsync(Guid tenantId, string template, string content, CancellationToken ct)
    {
        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            template,
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
