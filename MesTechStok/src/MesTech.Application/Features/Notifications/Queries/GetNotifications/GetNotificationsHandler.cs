using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Bildirim listesi handler'i.
/// Sayfalanmis bildirim kayitlarini repository'den ceker ve DTO'ya donusturur.
/// </summary>
public sealed class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, NotificationListResult>
{
    private readonly INotificationLogRepository _repository;

    public GetNotificationsHandler(INotificationLogRepository repository)
        => _repository = repository;

    public async Task<NotificationListResult> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            request.UnreadOnly,
            cancellationToken);

        var dtos = items.Select(n => new NotificationDto
        {
            Id = n.Id,
            TenantId = n.TenantId,
            Channel = n.Channel.ToString(),
            Recipient = n.Recipient,
            TemplateName = n.TemplateName,
            Content = n.Content,
            Status = n.Status.ToString(),
            SentAt = n.SentAt,
            DeliveredAt = n.DeliveredAt,
            ReadAt = n.ReadAt,
            ErrorMessage = n.ErrorMessage,
            CreatedAt = n.CreatedAt
        }).ToList().AsReadOnly();

        return new NotificationListResult(dtos, totalCount, request.Page, request.PageSize);
    }
}
