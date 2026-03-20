using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;

/// <summary>
/// Kullanici bildirim ayarlari handler'i.
/// UserId bazinda tum NotificationSetting kayitlarini ceker ve DTO'ya donusturur.
/// </summary>
public class GetNotificationSettingsHandler
    : IRequestHandler<GetNotificationSettingsQuery, IReadOnlyList<NotificationSettingDto>>
{
    private readonly INotificationSettingRepository _repository;

    public GetNotificationSettingsHandler(INotificationSettingRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<NotificationSettingDto>> Handle(
        GetNotificationSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        var dtos = settings.Select(s => new NotificationSettingDto
        {
            Id = s.Id,
            UserId = s.UserId,
            Channel = s.Channel.ToString(),
            IsEnabled = s.IsEnabled,
            NotifyOnOrderReceived = s.NotifyOnOrderReceived,
            NotifyOnLowStock = s.NotifyOnLowStock,
            LowStockThreshold = s.LowStockThreshold,
            NotifyOnInvoiceDue = s.NotifyOnInvoiceDue,
            NotifyOnPaymentReceived = s.NotifyOnPaymentReceived,
            NotifyOnPlatformMessage = s.NotifyOnPlatformMessage,
            NotifyOnAIInsight = s.NotifyOnAIInsight,
            NotifyOnBuyboxLost = s.NotifyOnBuyboxLost,
            NotifyOnSystemError = s.NotifyOnSystemError,
            NotifyOnTaxDeadline = s.NotifyOnTaxDeadline,
            NotifyOnReportReady = s.NotifyOnReportReady,
            QuietHoursStart = s.QuietHoursStart?.ToString(),
            QuietHoursEnd = s.QuietHoursEnd?.ToString(),
            PreferredLanguage = s.PreferredLanguage,
            DigestMode = s.DigestMode,
            DigestTime = s.DigestTime?.ToString()
        }).ToList().AsReadOnly();

        return dtos;
    }
}
