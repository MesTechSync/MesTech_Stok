using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;

/// <summary>
/// Kullanici ici bildirimi okundu olarak isaretleme handler'i.
/// </summary>
public sealed class MarkUserNotificationReadHandler
    : IRequestHandler<MarkUserNotificationReadCommand, bool>
{
    private readonly IUserNotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkUserNotificationReadHandler(
        IUserNotificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        MarkUserNotificationReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null || notification.TenantId != request.TenantId)
            return false;

        notification.MarkAsRead();

        await _repository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
