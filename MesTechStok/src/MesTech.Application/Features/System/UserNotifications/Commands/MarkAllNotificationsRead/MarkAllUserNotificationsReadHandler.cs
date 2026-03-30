using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;

/// <summary>
/// Kullanicinin tum bildirimlerini okundu olarak isaretleme handler'i.
/// Okunmamis tum bildirimler icin MarkAsRead() cagrilir.
/// </summary>
public sealed class MarkAllUserNotificationsReadHandler
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
        ArgumentNullException.ThrowIfNull(request);

        // Bulk update — single SQL query instead of N+1 (fetch all + update each)
        return await _repository.MarkAllAsReadAsync(
            request.TenantId, request.UserId, cancellationToken);
    }
}
