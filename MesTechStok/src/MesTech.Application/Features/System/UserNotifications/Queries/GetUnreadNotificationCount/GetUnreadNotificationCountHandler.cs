using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;

/// <summary>
/// Okunmamis bildirim sayisi handler'i.
/// </summary>
public sealed class GetUnreadNotificationCountHandler
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IUserNotificationRepository _repository;

    public GetUnreadNotificationCountHandler(IUserNotificationRepository repository)
        => _repository = repository;

    public async Task<int> Handle(
        GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _repository.GetUnreadCountAsync(
            request.TenantId,
            request.UserId,
            cancellationToken);
    }
}
