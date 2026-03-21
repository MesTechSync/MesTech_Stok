using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;

/// <summary>
/// Kullanici ici bildirim listesi handler'i.
/// IUserNotificationRepository uzerinden sayfalanmis bildirim kayitlarini ceker.
/// </summary>
public class GetUserNotificationsHandler
    : IRequestHandler<GetUserNotificationsQuery, UserNotificationListResult>
{
    private readonly IUserNotificationRepository _repository;

    public GetUserNotificationsHandler(IUserNotificationRepository repository)
        => _repository = repository;

    public async Task<UserNotificationListResult> Handle(
        GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId,
            request.UserId,
            request.Page,
            request.PageSize,
            request.UnreadOnly,
            cancellationToken);

        var dtos = items.Select(n => new UserNotificationDto
        {
            Id = n.Id,
            TenantId = n.TenantId,
            UserId = n.UserId,
            Title = n.Title,
            Message = n.Message,
            Category = n.Category.ToString(),
            ActionUrl = n.ActionUrl,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt
        }).ToList().AsReadOnly();

        return new UserNotificationListResult(dtos, totalCount, request.Page, request.PageSize);
    }
}
