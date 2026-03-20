using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;

/// <summary>
/// Kullanici bildirim ayarlari sorgusu.
/// Belirtilen kullanicinin tum kanal tercihleri dondurulur.
/// </summary>
public record GetNotificationSettingsQuery(
    Guid TenantId,
    Guid UserId
) : IRequest<IReadOnlyList<NotificationSettingDto>>;
