using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Abonelik yaşam döngüsü bildirimleri — Created/Cancelled/PlanChanged.
/// </summary>
public interface ISubscriptionNotificationHandler
{
    Task HandleCreatedAsync(Guid subscriptionId, Guid tenantId, CancellationToken ct);
    Task HandleCancelledAsync(Guid subscriptionId, Guid tenantId, string? reason, CancellationToken ct);
    Task HandlePlanChangedAsync(Guid subscriptionId, Guid tenantId, string newPlan, CancellationToken ct);
}

public sealed class SubscriptionNotificationHandler : ISubscriptionNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SubscriptionNotificationHandler> _logger;

    public SubscriptionNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<SubscriptionNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleCreatedAsync(Guid subscriptionId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation("SubscriptionCreated → bildirim. SubId={Id}", subscriptionId);
        await CreateNotificationAsync(tenantId, "SubscriptionCreated",
            $"Yeni abonelik oluşturuldu — ID: {subscriptionId}", ct);
    }

    public async Task HandleCancelledAsync(Guid subscriptionId, Guid tenantId, string? reason, CancellationToken ct)
    {
        _logger.LogWarning("SubscriptionCancelled → bildirim. SubId={Id}, Reason={Reason}", subscriptionId, reason);
        await CreateNotificationAsync(tenantId, "SubscriptionCancelled",
            $"Abonelik iptal edildi — ID: {subscriptionId}, Sebep: {reason ?? "belirtilmedi"}", ct);
    }

    public async Task HandlePlanChangedAsync(Guid subscriptionId, Guid tenantId, string newPlan, CancellationToken ct)
    {
        _logger.LogInformation("SubscriptionPlanChanged → bildirim. SubId={Id}, Plan={Plan}", subscriptionId, newPlan);
        await CreateNotificationAsync(tenantId, "SubscriptionPlanChanged",
            $"Abonelik planı değiştirildi — Yeni Plan: {newPlan}", ct);
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
