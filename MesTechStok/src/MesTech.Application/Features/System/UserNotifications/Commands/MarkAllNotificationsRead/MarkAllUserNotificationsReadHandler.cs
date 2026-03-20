using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;

/// <summary>
/// Kullanicinin tum bildirimlerini okundu olarak isaretleme handler'i.
/// Okunmamis tum bildirimler icin MarkAsRead() cagrilir.
/// </summary>
public class MarkAllUserNotificationsReadHandler
    : IRequestHandler<MarkAllUserNotificationsReadCommand, int>
{
    private readonly IUserNotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllUserNotificationsReadHandler(
        IUserNotificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        MarkAllUserNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _repository.GetUnreadByUserAsync(
            request.TenantId,
            request.UserId,
            cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
            await _repository.UpdateAsync(notification, cancellationToken);
        }

        if (unread.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return unread.Count;
    }
}
