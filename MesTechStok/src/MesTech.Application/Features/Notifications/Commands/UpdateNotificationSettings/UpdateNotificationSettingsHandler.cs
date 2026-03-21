using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;

/// <summary>
/// Bildirim ayarlari guncelleme handler'i.
/// Mevcut User+Channel cifti varsa gunceller, yoksa yeni kayit olusturur (upsert).
/// ChannelAddress PII — log'a yazilmaz.
/// </summary>
public class UpdateNotificationSettingsHandler : IRequestHandler<UpdateNotificationSettingsCommand, Guid>
{
    private readonly INotificationSettingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateNotificationSettingsHandler> _logger;

    public UpdateNotificationSettingsHandler(
        INotificationSettingRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateNotificationSettingsHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(UpdateNotificationSettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _repository.GetByUserAndChannelAsync(
            request.UserId, request.Channel, cancellationToken);

        if (existing is not null)
        {
            ApplyUpdates(existing, request);
            existing.MarkUpdated();
            await _repository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[NotificationSettings] Guncellendi: UserId={UserId}, Channel={Channel}, IsEnabled={IsEnabled}",
                request.UserId, request.Channel, request.IsEnabled);

            return existing.Id;
        }

        // Create new setting
        var setting = new NotificationSetting
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Channel = request.Channel,
            ChannelAddress = request.ChannelAddress,
            IsEnabled = request.IsEnabled
        };

        ApplyUpdates(setting, request);
        setting.MarkUpdated();

        await _repository.AddAsync(setting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[NotificationSettings] Olusturuldu: UserId={UserId}, Channel={Channel}, IsEnabled={IsEnabled}",
            request.UserId, request.Channel, request.IsEnabled);

        return setting.Id;
    }

    private static void ApplyUpdates(NotificationSetting setting, UpdateNotificationSettingsCommand request)
    {
        setting.ChannelAddress = request.ChannelAddress;
        setting.IsEnabled = request.IsEnabled;
        setting.NotifyOnOrderReceived = request.NotifyOnOrderReceived;
        setting.NotifyOnLowStock = request.NotifyOnLowStock;
        setting.LowStockThreshold = request.LowStockThreshold;
        setting.NotifyOnInvoiceDue = request.NotifyOnInvoiceDue;
        setting.NotifyOnPaymentReceived = request.NotifyOnPaymentReceived;
        setting.NotifyOnPlatformMessage = request.NotifyOnPlatformMessage;
        setting.NotifyOnAIInsight = request.NotifyOnAIInsight;
        setting.NotifyOnBuyboxLost = request.NotifyOnBuyboxLost;
        setting.NotifyOnSystemError = request.NotifyOnSystemError;
        setting.NotifyOnTaxDeadline = request.NotifyOnTaxDeadline;
        setting.NotifyOnReportReady = request.NotifyOnReportReady;
        setting.PreferredLanguage = request.PreferredLanguage;
        setting.DigestMode = request.DigestMode;

        setting.QuietHoursStart = string.IsNullOrWhiteSpace(request.QuietHoursStart)
            ? null
            : TimeOnly.Parse(request.QuietHoursStart);

        setting.QuietHoursEnd = string.IsNullOrWhiteSpace(request.QuietHoursEnd)
            ? null
            : TimeOnly.Parse(request.QuietHoursEnd);

        setting.DigestTime = string.IsNullOrWhiteSpace(request.DigestTime)
            ? null
            : TimeOnly.Parse(request.DigestTime);
    }
}
