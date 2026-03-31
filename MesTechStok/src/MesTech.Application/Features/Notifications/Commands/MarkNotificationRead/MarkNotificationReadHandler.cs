using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Bildirimi okundu olarak isaretleme handler'i.
/// NotificationLog entity'sini yukler, MarkAsRead() cagrisini yapar ve kaydeder.
/// </summary>
public sealed class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly INotificationLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationReadHandler(INotificationLogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken).ConfigureAwait(false);

        if (notification == null || notification.TenantId != request.TenantId)
            return false;

        notification.MarkAsRead();

        await _repository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
